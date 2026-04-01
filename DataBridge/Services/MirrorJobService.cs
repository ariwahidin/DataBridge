using DataBridge.Models.Entities;
using DataBridge.Models.Enums;
using DataBridge.Models.ViewModels.MirrorJobs;
using DataBridge.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace DataBridge.Services
{
    public class MirrorJobService
    {
        private readonly IMirrorJobRepository _jobRepo;
        private readonly ISourceRepository _sourceRepo;

        public MirrorJobService(IMirrorJobRepository jobRepo, ISourceRepository sourceRepo)
        {
            _jobRepo = jobRepo;
            _sourceRepo = sourceRepo;
        }

        public async Task<MirrorJobListViewModel> GetListAsync(string? search)
        {
            var all = await _jobRepo.GetAllWithSourceAsync();
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                all = all.Where(j => j.Name.ToLower().Contains(search) ||
                    j.Source.Name.ToLower().Contains(search) ||
                    j.DestinationTable.ToLower().Contains(search)).ToList();
            }
            return new MirrorJobListViewModel
            {
                Jobs = all.Select(j => new MirrorJobRowViewModel
                {
                    Id = j.Id,
                    Name = j.Name,
                    SourceName = j.Source.Name,
                    DestinationTable = j.DestinationTable,
                    SyncMode = j.SyncMode,
                    IsActive = j.IsActive,
                    HasSchedule = j.Schedule != null,
                    ScheduleEnabled = j.Schedule?.IsEnabled ?? false,
                    CronExpression = j.Schedule?.CronExpression,
                    LastRunAt = j.Schedule?.LastRunAt,
                    CreatedAt = j.CreatedAt,
                }).ToList(),
                Search = search,
            };
        }

        public async Task<MirrorJobFormViewModel> GetEmptyFormAsync()
        {
            var sources = await _sourceRepo.GetAllAsync();
            return new MirrorJobFormViewModel
            {
                AvailableSources = sources.Where(s => s.IsActive)
                    .Select(s => new SourceOption { Id = s.Id, Name = s.Name }).ToList()
            };
        }

        public async Task<MirrorJobFormViewModel?> GetForEditAsync(int id)
        {
            var job = await _jobRepo.GetByIdWithDetailsAsync(id);
            if (job == null) return null;
            var sources = await _sourceRepo.GetAllAsync();
            return new MirrorJobFormViewModel
            {
                Id = job.Id,
                Name = job.Name,
                SourceId = job.SourceId,
                SourceQuery = job.SourceQuery,
                DestinationTable = job.DestinationTable,
                SyncMode = job.SyncMode,
                WatermarkColumn = job.WatermarkColumn,
                IsActive = job.IsActive,
                AvailableSources = sources.Where(s => s.IsActive)
                    .Select(s => new SourceOption { Id = s.Id, Name = s.Name }).ToList(),
            };
        }

        public async Task<(bool Success, string? Error)> CreateAsync(MirrorJobFormViewModel vm)
        {
            var job = new MirrorJob
            {
                Name = vm.Name.Trim(),
                SourceId = vm.SourceId,
                SourceQuery = vm.SourceQuery.Trim(),
                DestinationTable = vm.DestinationTable.Trim(),
                SyncMode = vm.SyncMode,
                WatermarkColumn = vm.SyncMode == SyncMode.Incremental ? vm.WatermarkColumn?.Trim() : null,
                IsActive = vm.IsActive,
                CreatedAt = DateTime.Now,
            };
            await _jobRepo.AddAsync(job);
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(MirrorJobFormViewModel vm)
        {
            var job = await _jobRepo.GetByIdWithDetailsAsync(vm.Id);
            if (job == null) return (false, "Job not found.");

            job.Name = vm.Name.Trim();
            job.SourceId = vm.SourceId;
            job.SourceQuery = vm.SourceQuery.Trim();
            job.DestinationTable = vm.DestinationTable.Trim();
            job.SyncMode = vm.SyncMode;
            job.WatermarkColumn = vm.SyncMode == SyncMode.Incremental ? vm.WatermarkColumn?.Trim() : null;
            job.IsActive = vm.IsActive;

            await _jobRepo.UpdateAsync(job);
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> DeleteAsync(int id)
        {
            var job = await _jobRepo.GetByIdWithDetailsAsync(id);
            if (job == null) return (false, "Job not found.");
            await _jobRepo.DeleteAsync(id);
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> ToggleActiveAsync(int id)
        {
            var job = await _jobRepo.GetByIdWithDetailsAsync(id);
            if (job == null) return (false, "Job not found.");
            job.IsActive = !job.IsActive;
            await _jobRepo.UpdateAsync(job);
            return (true, null);
        }

        // ── NEW: Manual trigger ─────────────────────────────────────────────────
        /// <summary>
        /// Manually triggers a mirror job run (placeholder – wire up your real
        /// execution logic / background service here).
        /// </summary>
        public async Task<(bool Success, string? Error)> TriggerRunAsync(int id)
        {
            var job = await _jobRepo.GetByIdWithDetailsAsync(id);
            if (job == null) return (false, "Job not found.");
            if (!job.IsActive) return (false, "Job is inactive. Activate it before triggering.");

            // TODO: enqueue to your actual job runner / Hangfire / background service.
            // For now we just return success so the UI wiring is in place.
            await Task.CompletedTask;
            return (true, null);
        }

        // ── NEW: Validate source query ──────────────────────────────────────────
        /// <summary>
        /// Validates that <paramref name="sql"/> is safe (SELECT / CTE only) and
        /// optionally test-executes it against the source connection string.
        /// Returns (true, null) on success, or (false, errorMessage) on failure.
        /// </summary>
        public async Task<(bool Success, string? Error, int? RowCount)> ValidateSourceQueryAsync(
            string connectionString, string sql)
        {
            // ── 1. Static safety check ──────────────────────────────────────────
            var safetyError = CheckQuerySafety(sql);
            if (safetyError != null)
                return (false, safetyError, null);

            // ── 2. Live execution test (TOP 1 wrapped) ──────────────────────────
            try
            {
                // Wrap user query so we never fetch more than 1 row during a test.
                var wrappedSql = $"SELECT TOP 1 * FROM ({sql}) AS __test__";

                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand(wrappedSql, conn);
                cmd.CommandTimeout = 30;

                // Count columns as a lightweight "it ran" confirmation.
                using var reader = await cmd.ExecuteReaderAsync(
                    System.Data.CommandBehavior.SchemaOnly | System.Data.CommandBehavior.SingleRow);

                int colCount = reader.FieldCount;
                return (true, null, colCount);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        // ── Safety validator ────────────────────────────────────────────────────
        /// <summary>
        /// Returns an error string if the SQL contains any dangerous keywords,
        /// otherwise null. Supports CTEs (WITH ... AS ...) freely.
        /// </summary>
        public static string? CheckQuerySafety(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return "Query cannot be empty.";

            // Normalise: collapse whitespace, strip single-line comments,
            // strip block comments, then upper-case for keyword matching.
            var normalised = System.Text.RegularExpressions.Regex.Replace(sql, @"--[^\r\n]*", " ");
            normalised = System.Text.RegularExpressions.Regex.Replace(normalised, @"/\*.*?\*/", " ",
                System.Text.RegularExpressions.RegexOptions.Singleline);
            normalised = System.Text.RegularExpressions.Regex.Replace(normalised, @"\s+", " ").Trim().ToUpper();

            // Forbidden statement-level keywords.
            // \b word boundary ensures e.g. "SELECTION" doesn't match "SELECT"... but
            // we actually DO want SELECT, so forbidden list is everything else.
            var forbidden = new[]
            {
                @"\bINSERT\b", @"\bUPDATE\b", @"\bDELETE\b", @"\bDROP\b",
                @"\bTRUNCATE\b", @"\bALTER\b", @"\bCREATE\b", @"\bEXEC\b",
                @"\bEXECUTE\b", @"\bSP_\w+", @"\bXP_\w+",
                @"\bGRANT\b", @"\bREVOKE\b", @"\bDENY\b",
                @"\bMERGE\b", @"\bBULK\b", @"\bOPENROWSET\b", @"\bOPENDATASOURCE\b",
            };

            foreach (var pattern in forbidden)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(normalised, pattern))
                {
                    // Extract the matched keyword for a helpful message.
                    var match = System.Text.RegularExpressions.Regex.Match(normalised, pattern);
                    return $"Forbidden keyword detected: '{match.Value}'. Only SELECT queries (including CTEs) are allowed.";
                }
            }

            // Must start with SELECT or WITH (CTE).
            if (!System.Text.RegularExpressions.Regex.IsMatch(normalised, @"^(SELECT|WITH)\b"))
                return "Query must start with SELECT or WITH (for CTEs).";

            return null;
        }
    }
}