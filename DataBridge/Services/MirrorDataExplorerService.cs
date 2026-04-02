using DataBridge.Models.ViewModels.MirrorTableBuilder;
using Microsoft.Data.SqlClient;
using System.Text;

namespace DataBridge.Services
{
    public class PagedDataResult
    {
        public List<string> Columns { get; set; } = new();
        public List<Dictionary<string, object?>> Rows { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public class MirrorDataExplorerService
    {
        private readonly string _connStr;
        private readonly ILogger<MirrorDataExplorerService> _logger;

        // Whitelist of safe sort directions
        private static readonly HashSet<string> AllowedSortDirs = new(StringComparer.OrdinalIgnoreCase)
            { "asc", "desc" };

        public MirrorDataExplorerService(IConfiguration config, ILogger<MirrorDataExplorerService> logger)
        {
            _connStr = config.GetConnectionString("MirrorConnection")
                       ?? throw new InvalidOperationException("MirrorConnection not configured.");
            _logger = logger;
        }

        // ── List tables (shared with builder) ─────────────────────────────────
        public async Task<List<TableInfoViewModel>> GetAllTablesAsync()
        {
            var result = new List<TableInfoViewModel>();
            const string sql = @"
                SELECT 
                    t.TABLE_SCHEMA,
                    t.TABLE_NAME,
                    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS c
                     WHERE c.TABLE_NAME = t.TABLE_NAME AND c.TABLE_SCHEMA = t.TABLE_SCHEMA) AS ColumnCount,
                    SUM(p.rows) AS TotalRows
                FROM INFORMATION_SCHEMA.TABLES t
                JOIN sys.objects obj ON obj.name = t.TABLE_NAME AND obj.type = 'U'
                JOIN sys.partitions p ON p.object_id = obj.object_id AND p.index_id IN (0,1)
                WHERE t.TABLE_TYPE = 'BASE TABLE'
                GROUP BY t.TABLE_SCHEMA, t.TABLE_NAME
                ORDER BY t.TABLE_NAME";

            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new TableInfoViewModel
                {
                    TableName = $"{reader.GetString(0)}.{reader.GetString(1)}",
                    ColumnCount = reader.GetInt32(2),
                    RowCount = reader.IsDBNull(3) ? 0 : Convert.ToInt64(reader.GetValue(3))
                });
            }

            return result;
        }

        // ── Get columns ───────────────────────────────────────────────────────
        public async Task<List<ColumnInfoViewModel>> GetColumnsAsync(string fullTableName)
        {
            var parts = ParseTableName(fullTableName);
            var result = new List<ColumnInfoViewModel>();

            const string sql = @"
                SELECT c.COLUMN_NAME, c.DATA_TYPE, c.IS_NULLABLE, c.ORDINAL_POSITION
                FROM INFORMATION_SCHEMA.COLUMNS c
                WHERE c.TABLE_SCHEMA = @schema AND c.TABLE_NAME = @table
                ORDER BY c.ORDINAL_POSITION";

            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@schema", parts.schema);
            cmd.Parameters.AddWithValue("@table", parts.table);

            Console.WriteLine("Executing query...");
            await using var reader = await cmd.ExecuteReaderAsync();
            Console.WriteLine(reader.HasRows);
            while (await reader.ReadAsync())
            {
                result.Add(new ColumnInfoViewModel
                {
                    ColumnName = reader.GetString(0),
                    DataType = reader.GetString(1).ToUpper(),
                    IsNullable = reader.GetString(2) == "YES",
                    OrdinalPosition = reader.GetInt32(3)
                });
            }

            return result;
        }

        // ── Paged data query ──────────────────────────────────────────────────
        public async Task<PagedDataResult> GetPagedDataAsync(
            string fullTableName,
            string? search,
            string? filterColumn,
            string? filterValue,
            string? sortColumn,
            string sortDir,
            int page,
            int pageSize)
        {
            var parts = ParseTableName(fullTableName);

            // Validate table exists (prevent SQL injection via table name)
            var columns = await GetColumnsAsync(fullTableName);
            if (!columns.Any())
                return new PagedDataResult();

            var columnNames = columns.Select(c => c.ColumnName).ToList();

            // Validate sort column
            if (sortColumn != null && !columnNames.Contains(sortColumn, StringComparer.OrdinalIgnoreCase))
                sortColumn = null;

            // Validate filter column
            if (filterColumn != null && !columnNames.Contains(filterColumn, StringComparer.OrdinalIgnoreCase))
                filterColumn = null;

            // Validate sort dir
            if (!AllowedSortDirs.Contains(sortDir)) sortDir = "asc";

            // Build WHERE clause
            var whereParts = new List<string>();
            var sqlParams = new List<SqlParameter>();

            // Global search across all string columns
            if (!string.IsNullOrWhiteSpace(search))
            {
                var stringCols = columns
                    .Where(c => IsStringType(c.DataType))
                    .Select(c => $"CAST([{c.ColumnName}] AS NVARCHAR(MAX)) LIKE @search")
                    .ToList();

                if (stringCols.Any())
                {
                    whereParts.Add($"({string.Join(" OR ", stringCols)})");
                    sqlParams.Add(new SqlParameter("@search", $"%{search}%"));
                }
            }

            // Column filter
            if (!string.IsNullOrWhiteSpace(filterColumn) && filterValue != null)
            {
                whereParts.Add($"CAST([{filterColumn}] AS NVARCHAR(MAX)) LIKE @filterVal");
                sqlParams.Add(new SqlParameter("@filterVal", $"%{filterValue}%"));
            }

            var whereClause = whereParts.Any()
                ? "WHERE " + string.Join(" AND ", whereParts)
                : string.Empty;

            var orderBy = sortColumn != null
                ? $"ORDER BY [{sortColumn}] {sortDir.ToUpper()}"
                : $"ORDER BY (SELECT NULL)";

            var offset = (page - 1) * pageSize;

            var countSql = $"SELECT COUNT(*) FROM [{parts.schema}].[{parts.table}] {whereClause}";
            var dataSql = $@"
                SELECT *
                FROM [{parts.schema}].[{parts.table}]
                {whereClause}
                {orderBy}
                OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";

            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();

            // Total count
            await using var countCmd = new SqlCommand(countSql, conn);
            foreach (var p in sqlParams) countCmd.Parameters.Add(CloneParam(p));
            var countScalar = await countCmd.ExecuteScalarAsync();
            var totalCount = countScalar == null || countScalar == DBNull.Value
                ? 0 : Convert.ToInt32(countScalar);

            // Data
            var rows = new List<Dictionary<string, object?>>();
            await using var dataCmd = new SqlCommand(dataSql, conn);
            foreach (var p in sqlParams) dataCmd.Parameters.Add(CloneParam(p));
            await using var reader = await dataCmd.ExecuteReaderAsync();

            var colList = Enumerable.Range(0, reader.FieldCount)
                .Select(i => reader.GetName(i))
                .ToList();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (reader.IsDBNull(i)) { row[colList[i]] = null; continue; }
                    var val = reader.GetValue(i);
                    // Normalize to JSON-safe types
                    row[colList[i]] = val switch
                    {
                        DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
                        DateTimeOffset dto => dto.ToString("yyyy-MM-dd HH:mm:ss zzz"),
                        Guid g => g.ToString(),
                        byte[] => "(binary)",
                        bool b => b,
                        _ => val
                    };
                }
                rows.Add(row);
            }

            return new PagedDataResult
            {
                Columns = colList,
                Rows = rows,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static (string schema, string table) ParseTableName(string fullName)
        {
            var parts = fullName.Split('.', 2);
            return parts.Length == 2 ? (parts[0], parts[1]) : ("dbo", fullName);
        }

        private static bool IsStringType(string dataType) =>
            dataType is "NVARCHAR" or "VARCHAR" or "NCHAR" or "CHAR"
                     or "TEXT" or "NTEXT" or "UNIQUEIDENTIFIER" or "XML";

        private static SqlParameter CloneParam(SqlParameter p) =>
            new(p.ParameterName, p.Value);
    }
}