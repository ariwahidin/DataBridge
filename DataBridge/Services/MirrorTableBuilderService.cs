using DataBridge.Data;
using DataBridge.Models.Entities;
using DataBridge.Models.ViewModels.MirrorTableBuilder;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace DataBridge.Services
{
    public class MirrorTableBuilderService
    {
        //private readonly string _connStr;
        //private readonly ILogger<MirrorTableBuilderService> _logger;

        private readonly string _connStr;
        private readonly ILogger<MirrorTableBuilderService> _logger;
        private readonly AppDbContext _db;

        public MirrorTableBuilderService(
        IConfiguration config,
        ILogger<MirrorTableBuilderService> logger,
        AppDbContext db)
        {
            _connStr = config.GetConnectionString("MirrorConnection")
                       ?? throw new InvalidOperationException("MirrorConnection not configured.");
            _logger = logger;
            _db = db;
        }

        //public MirrorTableBuilderService(IConfiguration config, ILogger<MirrorTableBuilderService> logger)
        //{
        //    _connStr = config.GetConnectionString("MirrorConnection")
        //               ?? throw new InvalidOperationException("MirrorConnection not configured.");
        //    _logger = logger;
        //}

        // ── List all user tables ──────────────────────────────────────────────
        public async Task<List<TableInfoViewModel>> GetAllTablesAsync()
        {
            var registeredTables = await _db.MirrorTableRegistries
                .OrderBy(r => r.TableName)
                .ToListAsync();

            if (!registeredTables.Any())
                return new List<TableInfoViewModel>();

            var result = new List<TableInfoViewModel>();

            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();

            foreach (var reg in registeredTables)
            {
                var info = new TableInfoViewModel
                {
                    TableName = reg.FullName,
                    CreatedDate = reg.CreatedAt,
                };

                // Column count
                try
                {
                    await using var colCmd = new SqlCommand(
                        "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=@s AND TABLE_NAME=@t",
                        conn);
                    colCmd.Parameters.AddWithValue("@s", reg.Schema);
                    colCmd.Parameters.AddWithValue("@t", reg.TableName);
                    info.ColumnCount = (int)(await colCmd.ExecuteScalarAsync() ?? 0);
                }
                catch { }

                // Row count
                try
                {
                    await using var rowCmd = new SqlCommand(
                        "SELECT SUM(p.rows) FROM sys.partitions p JOIN sys.objects o ON p.object_id=o.object_id WHERE o.name=@t AND o.schema_id=SCHEMA_ID(@s) AND p.index_id IN (0,1)",
                        conn);
                    rowCmd.Parameters.AddWithValue("@s", reg.Schema);
                    rowCmd.Parameters.AddWithValue("@t", reg.TableName);
                    var cnt = await rowCmd.ExecuteScalarAsync();
                    info.RowCount = cnt == null || cnt == DBNull.Value ? 0 : Convert.ToInt64(cnt);
                }
                catch { info.RowCount = -1; }

                result.Add(info);
            }

            return result;
        }

        //public async Task<List<TableInfoViewModel>> GetAllTablesAsync()
        //{
        //    var result = new List<TableInfoViewModel>();
        //    const string sql = @"
        //        SELECT 
        //            t.TABLE_SCHEMA,
        //            t.TABLE_NAME,
        //            (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS c
        //             WHERE c.TABLE_NAME = t.TABLE_NAME AND c.TABLE_SCHEMA = t.TABLE_SCHEMA) AS ColumnCount,
        //            obj.create_date AS CreatedDate
        //        FROM INFORMATION_SCHEMA.TABLES t
        //        JOIN sys.objects obj ON obj.name = t.TABLE_NAME AND obj.type = 'U'
        //        WHERE t.TABLE_TYPE = 'BASE TABLE'
        //        ORDER BY t.TABLE_NAME";

        //    await using var conn = new SqlConnection(_connStr);
        //    await conn.OpenAsync();
        //    await using var cmd = new SqlCommand(sql, conn);
        //    await using var reader = await cmd.ExecuteReaderAsync();

        //    while (await reader.ReadAsync())
        //    {
        //        var schema = reader.GetString(0);
        //        var tableName = reader.GetString(1);
        //        result.Add(new TableInfoViewModel
        //        {
        //            TableName = $"{schema}.{tableName}",
        //            ColumnCount = reader.GetInt32(2),
        //            CreatedDate = reader.IsDBNull(3) ? null : reader.GetDateTime(3)
        //        });
        //    }

        //    // Get row counts separately (can't do it in one query easily across all tables)
        //    reader.Close();
        //    foreach (var t in result)
        //    {
        //        try
        //        {
        //            await using var countCmd = new SqlCommand(
        //                $"SELECT SUM(p.rows) FROM sys.partitions p JOIN sys.objects o ON p.object_id=o.object_id WHERE o.name=@n AND p.index_id IN (0,1)",
        //                conn);
        //            countCmd.Parameters.AddWithValue("@n", t.TableName.Split('.').Last());
        //            var cnt = await countCmd.ExecuteScalarAsync();
        //            t.RowCount = cnt == DBNull.Value ? 0 : Convert.ToInt64(cnt);
        //        }
        //        catch { t.RowCount = -1; }
        //    }

        //    return result;
        //}

        // ── Get columns of a table ────────────────────────────────────────────
        public async Task<List<ColumnInfoViewModel>> GetColumnsAsync(string fullTableName)
        {
            var parts = ParseTableName(fullTableName);
            var result = new List<ColumnInfoViewModel>();

            const string sql = @"
                SELECT
                    c.ORDINAL_POSITION,
                    c.COLUMN_NAME,
                    c.DATA_TYPE,
                    c.CHARACTER_MAXIMUM_LENGTH,
                    c.NUMERIC_PRECISION,
                    c.NUMERIC_SCALE,
                    c.IS_NULLABLE,
                    c.COLUMN_DEFAULT,
                    CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IsPK,
                    COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA+'.'+c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') AS IsIdentity
                FROM INFORMATION_SCHEMA.COLUMNS c
                LEFT JOIN (
                    SELECT ku.TABLE_SCHEMA, ku.TABLE_NAME, ku.COLUMN_NAME
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                        ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                        AND tc.TABLE_SCHEMA = ku.TABLE_SCHEMA
                    WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                ) pk ON pk.TABLE_SCHEMA = c.TABLE_SCHEMA
                      AND pk.TABLE_NAME = c.TABLE_NAME
                      AND pk.COLUMN_NAME = c.COLUMN_NAME
                WHERE c.TABLE_SCHEMA = @schema AND c.TABLE_NAME = @table
                ORDER BY c.ORDINAL_POSITION";

            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@schema", parts.schema);
            cmd.Parameters.AddWithValue("@table", parts.table);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var dataType = reader.GetString(2).ToUpper();
                var maxLen = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
                var precision = reader.IsDBNull(4) ? (int?)null : (int)reader.GetByte(4);
                var scale = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5);

                var fullType = BuildFullType(dataType, maxLen, precision, scale);

                result.Add(new ColumnInfoViewModel
                {
                    OrdinalPosition = reader.GetInt32(0),
                    ColumnName = reader.GetString(1),
                    DataType = dataType,
                    FullType = fullType,
                    IsNullable = reader.GetString(6) == "YES",
                    DefaultValue = reader.IsDBNull(7) ? null : reader.GetString(7),
                    IsPrimaryKey = reader.GetInt32(8) == 1,
                    IsIdentity = reader.GetInt32(9) == 1
                });
            }

            return result;
        }

        // ── Create table ──────────────────────────────────────────────────────

        public async Task<(bool Ok, string? Error)> CreateTableAsync(CreateTableViewModel vm)
        {
            try
            {
                var schema = string.IsNullOrWhiteSpace(vm.Schema) ? "dbo" : vm.Schema;
                var ddl = BuildCreateTableDdl(schema, vm.TableName, vm.Columns);

                _logger.LogInformation("Executing DDL: {Ddl}", ddl);

                await using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(ddl, conn);
                await cmd.ExecuteNonQueryAsync();

                // ── Save to registry ──────────────────────────────────────────
                var entry = new MirrorTableRegistry
                {
                    Schema = schema,
                    TableName = vm.TableName,
                    Description = vm.Description,   // lihat note di bawah *
                    CreatedBy = vm.CreatedBy,      // lihat note di bawah *
                    CreatedAt = DateTime.UtcNow,
                };
                _db.MirrorTableRegistries.Add(entry);
                await _db.SaveChangesAsync();
                // ─────────────────────────────────────────────────────────────

                return (true, null);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Failed to create table {Table}", vm.TableName);
                return (false, ex.Message);
            }
        }

        //public async Task<(bool Ok, string? Error)> CreateTableAsync(CreateTableViewModel vm)
        //{
        //    try
        //    {
        //        var schema = string.IsNullOrWhiteSpace(vm.Schema) ? "dbo" : vm.Schema;
        //        var ddl = BuildCreateTableDdl(schema, vm.TableName, vm.Columns);

        //        _logger.LogInformation("Executing DDL: {Ddl}", ddl);

        //        await using var conn = new SqlConnection(_connStr);
        //        await conn.OpenAsync();
        //        await using var cmd = new SqlCommand(ddl, conn);
        //        await cmd.ExecuteNonQueryAsync();
        //        return (true, null);
        //    }
        //    catch (SqlException ex)
        //    {
        //        _logger.LogError(ex, "Failed to create table {Table}", vm.TableName);
        //        return (false, ex.Message);
        //    }
        //}

        // ── Drop table ────────────────────────────────────────────────────────

        public async Task<(bool Ok, string? Error)> DropTableAsync(string fullTableName)
        {
            try
            {
                var parts = ParseTableName(fullTableName);

                // Cek di registry dulu (bukan di Mirror DB langsung)
                var regEntry = await _db.MirrorTableRegistries
                    .FirstOrDefaultAsync(r => r.Schema == parts.schema && r.TableName == parts.table);

                if (regEntry == null)
                    return (false, "Table not found in registry.");

                // Drop di Mirror DB
                var sql = $"DROP TABLE [{parts.schema}].[{parts.table}]";
                await using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync();

                // Hapus dari registry
                _db.MirrorTableRegistries.Remove(regEntry);
                await _db.SaveChangesAsync();

                return (true, null);
            }
            catch (SqlException ex)
            {
                return (false, ex.Message);
            }
        }

        //public async Task<(bool Ok, string? Error)> DropTableAsync(string fullTableName)
        //{
        //    try
        //    {
        //        // Validate table exists first
        //        var tables = await GetAllTablesAsync();
        //        if (!tables.Any(t => t.TableName.Equals(fullTableName, StringComparison.OrdinalIgnoreCase)))
        //            return (false, "Table not found.");

        //        var parts = ParseTableName(fullTableName);
        //        var sql = $"DROP TABLE [{parts.schema}].[{parts.table}]";

        //        await using var conn = new SqlConnection(_connStr);
        //        await conn.OpenAsync();
        //        await using var cmd = new SqlCommand(sql, conn);
        //        await cmd.ExecuteNonQueryAsync();
        //        return (true, null);
        //    }
        //    catch (SqlException ex)
        //    {
        //        return (false, ex.Message);
        //    }
        //}

        // ── Helpers ───────────────────────────────────────────────────────────
        private static string BuildCreateTableDdl(string schema, string tableName, List<ColumnDefinitionViewModel> cols)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE [{schema}].[{tableName}] (");

            var pkCols = cols.Where(c => c.IsPrimaryKey).Select(c => c.ColumnName).ToList();
            var lines = new List<string>();

            foreach (var col in cols)
            {
                var line = new StringBuilder();
                line.Append($"    [{col.ColumnName}] {BuildColType(col)}");

                if (col.IsIdentity && IsIntegerType(col.DataType))
                    line.Append(" IDENTITY(1,1)");

                if (col.IsPrimaryKey && pkCols.Count == 1)
                    line.Append(" PRIMARY KEY");

                line.Append(col.IsNullable ? " NULL" : " NOT NULL");

                if (!string.IsNullOrWhiteSpace(col.DefaultValue))
                    line.Append($" DEFAULT ({col.DefaultValue})");

                lines.Add(line.ToString());
            }

            // Composite PK
            if (pkCols.Count > 1)
            {
                lines.Add($"    CONSTRAINT [PK_{tableName}] PRIMARY KEY ({string.Join(", ", pkCols.Select(c => $"[{c}]"))})");
            }

            sb.Append(string.Join(",\n", lines));
            sb.AppendLine("\n);");
            return sb.ToString();
        }

        private static string BuildColType(ColumnDefinitionViewModel col)
        {
            return col.DataType.ToUpper() switch
            {
                "NVARCHAR" or "VARCHAR" or "NCHAR" or "CHAR" =>
                    $"{col.DataType}({(col.Length.HasValue && col.Length > 0 ? col.Length.ToString() : "MAX")})",
                "VARBINARY" or "BINARY" =>
                    $"{col.DataType}({(col.Length.HasValue && col.Length > 0 ? col.Length.ToString() : "MAX")})",
                "DECIMAL" or "NUMERIC" =>
                    $"{col.DataType}({col.Precision ?? 18},{col.Scale ?? 2})",
                _ => col.DataType
            };
        }

        private static string BuildFullType(string dataType, int? maxLen, int? precision, int? scale)
        {
            return dataType switch
            {
                "NVARCHAR" or "VARCHAR" or "NCHAR" or "CHAR" or "VARBINARY" or "BINARY" =>
                    maxLen == -1 ? $"{dataType}(MAX)" : $"{dataType}({maxLen})",
                "DECIMAL" or "NUMERIC" =>
                    $"{dataType}({precision},{scale})",
                _ => dataType
            };
        }

        private static bool IsIntegerType(string dt) =>
            dt is "INT" or "BIGINT" or "SMALLINT" or "TINYINT";

        private static (string schema, string table) ParseTableName(string fullName)
        {
            var parts = fullName.Split('.', 2);
            return parts.Length == 2 ? (parts[0], parts[1]) : ("dbo", fullName);
        }

        public async Task<EditSchemaViewModel?> GetEditSchemaAsync(string fullTableName)
        {
            var parts = ParseTableName(fullTableName);
            var cols = await GetColumnsAsync(fullTableName);

            if (!cols.Any()) return null;

            return new EditSchemaViewModel
            {
                FullTableName = fullTableName,
                Schema = parts.schema,
                TableName = parts.table,
                ExistingColumns = cols.Select(c => new ExistingColumnViewModel
                {
                    ColumnName = c.ColumnName,
                    FullType = c.FullType,
                    DataType = c.DataType,
                    IsNullable = c.IsNullable,
                    NewIsNullable = c.IsNullable,
                    IsPrimaryKey = c.IsPrimaryKey,
                    IsIdentity = c.IsIdentity,
                }).ToList(),
                NewColumns = new List<ColumnDefinitionViewModel>()
            };
        }

        // ── Apply schema edits ────────────────────────────────────────────────────
        public async Task<(bool Ok, string? Error)> ApplySchemaEditAsync(EditSchemaViewModel vm)
        {
            var errors = new List<string>();

            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();

            // 1. DROP kolom yang ditandai
            foreach (var col in vm.ExistingColumns.Where(c => c.MarkedForDrop))
            {
                if (col.IsPrimaryKey)
                {
                    errors.Add($"Cannot drop primary key column [{col.ColumnName}].");
                    continue;
                }
                if (col.IsIdentity)
                {
                    errors.Add($"Cannot drop identity column [{col.ColumnName}].");
                    continue;
                }

                try
                {
                    // Hapus default constraint dulu kalau ada
                    var dropDefaultSql = $@"
                DECLARE @con NVARCHAR(256);
                SELECT @con = dc.name
                FROM sys.default_constraints dc
                JOIN sys.columns sc ON dc.parent_object_id = sc.object_id
                                    AND dc.parent_column_id = sc.column_id
                WHERE sc.object_id = OBJECT_ID('{vm.Schema}.{vm.TableName}')
                  AND sc.name = '{col.ColumnName}';
                IF @con IS NOT NULL
                    EXEC('ALTER TABLE [{vm.Schema}].[{vm.TableName}] DROP CONSTRAINT [' + @con + ']');";

                    await using var dropDefaultCmd = new SqlCommand(dropDefaultSql, conn) { CommandTimeout = 60 };
                    await dropDefaultCmd.ExecuteNonQueryAsync();

                    var dropSql = $"ALTER TABLE [{vm.Schema}].[{vm.TableName}] DROP COLUMN [{col.ColumnName}]";
                    await using var dropCmd = new SqlCommand(dropSql, conn) { CommandTimeout = 60 };
                    await dropCmd.ExecuteNonQueryAsync();

                    _logger.LogInformation("Dropped column [{Col}] from [{Table}]", col.ColumnName, vm.FullTableName);
                }
                catch (SqlException ex)
                {
                    errors.Add($"Drop [{col.ColumnName}]: {ex.Message}");
                }
            }

            // 2. ALTER kolom nullable/not-null (hanya kalau berbeda & bukan identity/PK)
            foreach (var col in vm.ExistingColumns.Where(c => !c.MarkedForDrop && c.IsNullable != c.NewIsNullable))
            {
                if (col.IsIdentity || col.IsPrimaryKey) continue;  // tidak bisa di-alter

                try
                {
                    // Perlu tahu full type untuk ALTER COLUMN
                    var alterSql = $"ALTER TABLE [{vm.Schema}].[{vm.TableName}] ALTER COLUMN [{col.ColumnName}] {col.FullType} {(col.NewIsNullable ? "NULL" : "NOT NULL")}";
                    await using var alterCmd = new SqlCommand(alterSql, conn) { CommandTimeout = 60 };
                    await alterCmd.ExecuteNonQueryAsync();

                    _logger.LogInformation("Altered nullable [{Col}] on [{Table}] -> {Null}",
                        col.ColumnName, vm.FullTableName, col.NewIsNullable ? "NULL" : "NOT NULL");
                }
                catch (SqlException ex)
                {
                    errors.Add($"Alter [{col.ColumnName}]: {ex.Message}");
                }
            }

            // 3. ADD kolom baru
            var newCols = (vm.NewColumns ?? new())
                .Where(c => !string.IsNullOrWhiteSpace(c.ColumnName))
                .ToList();

            foreach (var col in newCols)
            {
                try
                {
                    var colType = BuildColType(col);
                    var nullClause = col.IsNullable ? "NULL" : "NOT NULL";
                    var defClause = string.IsNullOrWhiteSpace(col.DefaultValue) ? "" : $" DEFAULT ({col.DefaultValue})";

                    var addSql = $"ALTER TABLE [{vm.Schema}].[{vm.TableName}] ADD [{col.ColumnName}] {colType} {nullClause}{defClause}";
                    await using var addCmd = new SqlCommand(addSql, conn) { CommandTimeout = 60 };
                    await addCmd.ExecuteNonQueryAsync();

                    _logger.LogInformation("Added column [{Col}] to [{Table}]", col.ColumnName, vm.FullTableName);
                }
                catch (SqlException ex)
                {
                    errors.Add($"Add [{col.ColumnName}]: {ex.Message}");
                }
            }

            if (errors.Any())
                return (false, string.Join("\n", errors));

            return (true, null);
        }

        // ── Rename table ──────────────────────────────────────────────────────────
        public async Task<(bool Ok, string? Error)> RenameTableAsync(string fullTableName, string newName)
        {
            try
            {
                var parts = ParseTableName(fullTableName);

                // Rename di Mirror DB pakai sp_rename
                await using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();
                var renameSql = $"EXEC sp_rename '[{parts.schema}].[{parts.table}]', '{newName}'";
                await using var cmd = new SqlCommand(renameSql, conn) { CommandTimeout = 60 };
                await cmd.ExecuteNonQueryAsync();

                // Update registry
                var reg = await _db.MirrorTableRegistries
                    .FirstOrDefaultAsync(r => r.Schema == parts.schema && r.TableName == parts.table);
                if (reg != null)
                {
                    reg.TableName = newName;
                    await _db.SaveChangesAsync();
                }

                return (true, null);
            }
            catch (SqlException ex)
            {
                return (false, ex.Message);
            }
        }
    }
}