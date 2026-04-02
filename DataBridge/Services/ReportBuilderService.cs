using ClosedXML.Excel;
using DataBridge.Data;
using DataBridge.Models.Entities;
using DataBridge.Models.ViewModels.ReportBuilder;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.RegularExpressions;

namespace DataBridge.Services
{
    public class ReportBuilderService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<ReportBuilderService> _logger;
        private string MirrorConnStr => _config.GetConnectionString("MirrorConnection")!;

        public ReportBuilderService(AppDbContext db, IConfiguration config, ILogger<ReportBuilderService> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }

        // ── List ──────────────────────────────────────────────────────────────
        public async Task<List<ReportListItemViewModel>> GetAllAsync() =>
            await _db.ReportDefinitions
                .OrderBy(r => r.Name)
                .Select(r => new ReportListItemViewModel
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    CreatedAt = r.CreatedAt,
                    CreatedBy = r.CreatedBy,
                    ParamCount = r.Parameters.Count
                })
                .ToListAsync();

        public async Task<List<ReportListItemViewModel>> GetActiveAsync() =>
            await _db.ReportDefinitions
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .Select(r => new ReportListItemViewModel
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    CreatedAt = r.CreatedAt,
                    ParamCount = r.Parameters.Count
                })
                .ToListAsync();

        // ── Get for edit ──────────────────────────────────────────────────────
        public async Task<ReportFormViewModel?> GetFormAsync(int id)
        {
            var r = await _db.ReportDefinitions
                .Include(x => x.Parameters.OrderBy(p => p.SortOrder))
                .FirstOrDefaultAsync(x => x.Id == id);
            if (r == null) return null;

            return new ReportFormViewModel
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                SqlQuery = r.SqlQuery,
                IsActive = r.IsActive,
                Parameters = r.Parameters.Select(p => new ReportParamFormViewModel
                {
                    Id = p.Id,
                    ParamName = p.ParamName,
                    Label = p.Label,
                    ParamType = p.ParamType,
                    IsRequired = p.IsRequired,
                    DefaultValue = p.DefaultValue,
                    DropdownSource = p.DropdownSource,
                    SortOrder = p.SortOrder,
                }).ToList()
            };
        }

        // ── Save (create / update) ────────────────────────────────────────────
        public async Task<(bool Ok, string? Error)> SaveAsync(ReportFormViewModel vm, string? actor)
        {
            try
            {
                ReportDefinition entity;
                if (vm.Id == 0)
                {
                    entity = new ReportDefinition { CreatedAt = DateTime.UtcNow, CreatedBy = actor };
                    _db.ReportDefinitions.Add(entity);
                }
                else
                {
                    entity = await _db.ReportDefinitions
                        .Include(x => x.Parameters)
                        .FirstOrDefaultAsync(x => x.Id == vm.Id)
                        ?? throw new Exception("Report not found.");
                    // remove old params — re-insert below
                    _db.ReportParameters.RemoveRange(entity.Parameters);
                }

                entity.Name = vm.Name;
                entity.Description = vm.Description;
                entity.SqlQuery = vm.SqlQuery;
                entity.IsActive = vm.IsActive;

                var cleanParams = (vm.Parameters ?? new())
                    .Where(p => !string.IsNullOrWhiteSpace(p.ParamName))
                    .Select((p, idx) => new ReportParameter
                    {
                        ParamName = p.ParamName.Trim(),
                        Label = string.IsNullOrWhiteSpace(p.Label) ? p.ParamName : p.Label.Trim(),
                        ParamType = p.ParamType,
                        IsRequired = p.IsRequired,
                        DefaultValue = p.DefaultValue,
                        DropdownSource = p.DropdownSource,
                        SortOrder = idx,
                    }).ToList();

                entity.Parameters = cleanParams;
                await _db.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SaveReport failed");
                return (false, ex.Message);
            }
        }

        // ── Delete ────────────────────────────────────────────────────────────
        public async Task<(bool Ok, string? Error)> DeleteAsync(int id)
        {
            var r = await _db.ReportDefinitions.FindAsync(id);
            if (r == null) return (false, "Not found.");
            _db.ReportDefinitions.Remove(r);
            await _db.SaveChangesAsync();
            return (true, null);
        }

        // ── Get run viewmodel (end-user) ──────────────────────────────────────
        public async Task<ReportRunViewModel?> GetRunVmAsync(int id)
        {
            var r = await _db.ReportDefinitions
                .Include(x => x.Parameters.OrderBy(p => p.SortOrder))
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
            if (r == null) return null;

            var vm = new ReportRunViewModel
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Parameters = new List<ReportParamRunViewModel>()
            };

            foreach (var p in r.Parameters)
            {
                var pvm = new ReportParamRunViewModel
                {
                    Id = p.Id,
                    ParamName = p.ParamName,
                    Label = p.Label,
                    ParamType = p.ParamType,
                    IsRequired = p.IsRequired,
                    DefaultValue = p.DefaultValue,
                    DropdownSource = p.DropdownSource,
                };

                if (p.ParamType == ReportParamType.Dropdown && !string.IsNullOrWhiteSpace(p.DropdownSource))
                    pvm.DropdownOptions = await ResolveDropdownAsync(p.DropdownSource);

                vm.Parameters.Add(pvm);
            }
            return vm;
        }

        // ── Execute query ─────────────────────────────────────────────────────
        public async Task<ReportDataResult> ExecuteAsync(
            int reportId,
            Dictionary<string, string?> paramValues,
            int maxRows = 5000)
        {
            var r = await _db.ReportDefinitions
                .Include(x => x.Parameters)
                .FirstOrDefaultAsync(x => x.Id == reportId);

            if (r == null) return new ReportDataResult { Error = "Report not found." };

            return await RunQueryAsync(r.SqlQuery, r.Parameters.ToList(), paramValues, maxRows);
        }

        // ── Preview query (admin test) ────────────────────────────────────────
        public async Task<ReportDataResult> PreviewAsync(
            string rawSql,
            List<ReportParamFormViewModel> paramDefs,
            Dictionary<string, string?> paramValues,
            int maxRows = 200)
        {
            var paramEntities = paramDefs
                .Where(p => !string.IsNullOrWhiteSpace(p.ParamName))
                .Select(p => new ReportParameter
                {
                    ParamName = p.ParamName,
                    ParamType = p.ParamType,
                    IsRequired = p.IsRequired,
                }).ToList();

            return await RunQueryAsync(rawSql, paramEntities, paramValues, maxRows);
        }

        // ── Excel export ──────────────────────────────────────────────────────
        public async Task<byte[]> ExportExcelAsync(int reportId, Dictionary<string, string?> paramValues)
        {
            var result = await ExecuteAsync(reportId, paramValues, maxRows: 100_000);
            if (result.Error != null) throw new Exception(result.Error);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Report");

            // Header
            for (int c = 0; c < result.Columns.Count; c++)
            {
                var cell = ws.Cell(1, c + 1);
                cell.Value = result.Columns[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563eb");
                cell.Style.Font.FontColor = XLColor.White;
            }

            // Data
            for (int r = 0; r < result.Rows.Count; r++)
            {
                for (int c = 0; c < result.Columns.Count; c++)
                {
                    var val = result.Rows[r].GetValueOrDefault(result.Columns[c]);
                    ws.Cell(r + 2, c + 1).Value = val ?? "";
                }
            }

            ws.Columns().AdjustToContents(8, 60);
            ws.SheetView.FreezeRows(1);

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        // ── Internal: run query ───────────────────────────────────────────────
        private async Task<ReportDataResult> RunQueryAsync(
            string rawSql,
            List<ReportParameter> paramDefs,
            Dictionary<string, string?> paramValues,
            int maxRows)
        {
            try
            {
                // Replace {{ParamName}} → @ParamName in SQL
                var (finalSql, sqlParams) = BuildSql(rawSql, paramDefs, paramValues);

                // Wrap with TOP for safety
                var wrappedSql = $"SELECT TOP {maxRows + 1} * FROM ({finalSql}) AS __report__";

                await using var conn = new SqlConnection(MirrorConnStr);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(wrappedSql, conn) { CommandTimeout = 60 };
                foreach (var p in sqlParams) cmd.Parameters.Add(p);

                await using var reader = await cmd.ExecuteReaderAsync();

                var cols = Enumerable.Range(0, reader.FieldCount)
                    .Select(i => reader.GetName(i))
                    .ToList();

                var rows = new List<Dictionary<string, string?>>();
                while (await reader.ReadAsync())
                {
                    if (rows.Count >= maxRows) break;
                    var row = new Dictionary<string, string?>();
                    for (int i = 0; i < reader.FieldCount; i++)
                        row[cols[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i)?.ToString();
                    rows.Add(row);
                }

                // Check if there's more
                bool truncated = false;
                if (rows.Count == maxRows && await reader.ReadAsync())
                    truncated = true;

                return new ReportDataResult
                {
                    Columns = cols,
                    Rows = rows,
                    TotalRows = rows.Count,
                    Truncated = truncated
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Report query failed");
                return new ReportDataResult { Error = ex.Message };
            }
        }

        private static (string Sql, List<SqlParameter> Params) BuildSql(
            string rawSql,
            List<ReportParameter> paramDefs,
            Dictionary<string, string?> paramValues)
        {
            var sqlParams = new List<SqlParameter>();
            var finalSql = rawSql;

            foreach (var def in paramDefs)
            {
                var placeholder = $"{{{{{def.ParamName}}}}}";
                if (!finalSql.Contains(placeholder)) continue;

                paramValues.TryGetValue(def.ParamName, out var val);

                if (def.ParamType == ReportParamType.DateRange)
                {
                    // DateRange: split dengan |
                    var parts = (val ?? "").Split('|');
                    var from = parts.Length > 0 ? parts[0].Trim() : null;
                    var to = parts.Length > 1 ? parts[1].Trim() : null;

                    var pFrom = $"@p_{def.ParamName}_from";
                    var pTo = $"@p_{def.ParamName}_to";
                    finalSql = finalSql.Replace(placeholder, $"BETWEEN {pFrom} AND {pTo}");
                    sqlParams.Add(new SqlParameter(pFrom, (object?)ParseDate(from) ?? DBNull.Value));
                    sqlParams.Add(new SqlParameter(pTo, (object?)ParseDate(to) ?? DBNull.Value));
                }
                else
                {
                    var pName = $"@p_{def.ParamName}";
                    finalSql = finalSql.Replace(placeholder, pName);
                    object sqlVal = def.ParamType switch
                    {
                        ReportParamType.Number => decimal.TryParse(val, out var n) ? n : DBNull.Value,
                        ReportParamType.Date => ParseDate(val) ?? (object)DBNull.Value,
                        _ => string.IsNullOrWhiteSpace(val) ? DBNull.Value : val
                    };
                    sqlParams.Add(new SqlParameter(pName, sqlVal));
                }
            }

            return (finalSql, sqlParams);
        }

        private static DateTime? ParseDate(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            return DateTime.TryParse(s, out var dt) ? dt : null;
        }

        private async Task<List<string>> ResolveDropdownAsync(string source)
        {
            source = source.Trim();

            // JSON array: ["A","B","C"]
            if (source.StartsWith("["))
            {
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<string>>(source) ?? new();
                }
                catch { return new(); }
            }

            // SQL query — ambil kolom pertama
            try
            {
                await using var conn = new SqlConnection(MirrorConnStr);
                await conn.OpenAsync();
                //await using var cmd = new SqlCommand($"SELECT TOP 500 {source}", conn) { CommandTimeout = 30 };
                await using var cmd = new SqlCommand($"{source}", conn) { CommandTimeout = 30 };
                await using var reader = await cmd.ExecuteReaderAsync();
                var list = new List<string>();
                while (await reader.ReadAsync())
                    list.Add(reader.IsDBNull(0) ? "" : reader.GetValue(0).ToString()!);
                return list;
            }
            catch { return new(); }
        }
    }
}