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
        private readonly MirrorExecutionService _executor;
        private readonly QuartzSchedulerManager _quartz;

        public MirrorJobService(
            IMirrorJobRepository jobRepo,
            ISourceRepository sourceRepo,
            MirrorExecutionService executor,
            QuartzSchedulerManager quartz)
        {
            _jobRepo = jobRepo;
            _sourceRepo = sourceRepo;
            _executor = executor;
            _quartz = quartz;
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
                    CronLabel = j.Schedule != null
                        ? QuartzSchedulerManager.ToLabel(j.Schedule.CronExpression)
                        : null,
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
            await _quartz.UnscheduleJobAsync(id);
            await _jobRepo.DeleteAsync(id);
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> ToggleActiveAsync(int id)
        {
            var job = await _jobRepo.GetByIdWithDetailsAsync(id);
            if (job == null) return (false, "Job not found.");
            job.IsActive = !job.IsActive;
            await _jobRepo.UpdateAsync(job);

            // Sync Quartz
            if (!job.IsActive)
                await _quartz.UnscheduleJobAsync(id);
            else if (job.Schedule?.IsEnabled == true)
                await _quartz.ScheduleJobAsync(id, job.Schedule.CronExpression);

            return (true, null);
        }

        // ── REAL manual trigger ───────────────────────────────────────────────
        public async Task<(bool Success, string? Error)> TriggerRunAsync(int id)
        {
            var job = await _jobRepo.GetByIdWithDetailsAsync(id);
            if (job == null) return (false, "Job not found.");
            if (!job.IsActive) return (false, "Job is inactive. Activate it first.");

            // Fire via Quartz so [DisallowConcurrentExecution] is respected
            await _quartz.TriggerNowAsync(id);
            return (true, null);
        }

        // ── Query validation (unchanged) ──────────────────────────────────────
        public async Task<(bool Success, string? Error, int? RowCount)> ValidateSourceQueryAsync(
            string connectionString, string sql)
        {
            var safetyError = CheckQuerySafety(sql);
            if (safetyError != null) return (false, safetyError, null);

            try
            {
                var wrapped = $"SELECT TOP 1 * FROM ({sql}) AS __test__";
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(wrapped, conn) { CommandTimeout = 30 };
                using var reader = await cmd.ExecuteReaderAsync(
                    System.Data.CommandBehavior.SchemaOnly | System.Data.CommandBehavior.SingleRow);
                return (true, null, reader.FieldCount);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        public static string? CheckQuerySafety(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return "Query cannot be empty.";
            var n = System.Text.RegularExpressions.Regex.Replace(sql, @"--[^\r\n]*", " ");
            n = System.Text.RegularExpressions.Regex.Replace(n, @"/\*.*?\*/", " ",
                System.Text.RegularExpressions.RegexOptions.Singleline);
            n = System.Text.RegularExpressions.Regex.Replace(n, @"\s+", " ").Trim().ToUpper();

            var forbidden = new[] {
                @"\bINSERT\b",@"\bUPDATE\b",@"\bDELETE\b",@"\bDROP\b",@"\bTRUNCATE\b",
                @"\bALTER\b",@"\bCREATE\b",@"\bEXEC\b",@"\bEXECUTE\b",@"\bSP_\w+",
                @"\bXP_\w+",@"\bGRANT\b",@"\bREVOKE\b",@"\bDENY\b",@"\bMERGE\b",
                @"\bBULK\b",@"\bOPENROWSET\b",@"\bOPENDATASOURCE\b",
            };
            foreach (var p in forbidden)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(n, p))
                {
                    var m = System.Text.RegularExpressions.Regex.Match(n, p);
                    return $"Forbidden keyword: '{m.Value}'. Only SELECT/WITH (CTE) allowed.";
                }
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(n, @"^(SELECT|WITH)\b"))
                return "Query must start with SELECT or WITH.";
            return null;
        }
    }
}