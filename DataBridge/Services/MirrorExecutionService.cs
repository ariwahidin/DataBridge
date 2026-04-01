using DataBridge.Data;
using DataBridge.Models.Entities;
using DataBridge.Models.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace DataBridge.Services
{
    /// <summary>
    /// Core engine: reads from source DB, writes to mirror DB, logs history.
    /// </summary>
    public class MirrorExecutionService
    {
        //private readonly IDbContextFactory<AppDbContext> _ctxFactory;
        //private readonly IConfiguration _config;
        //private readonly ILogger<MirrorExecutionService> _logger;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<MirrorExecutionService> _logger;

        //public MirrorExecutionService(
        //    IDbContextFactory<AppDbContext> ctxFactory,
        //    IConfiguration config,
        //    ILogger<MirrorExecutionService> logger)
        //{
        //    _ctxFactory = ctxFactory;
        //    _config = config;
        //    _logger = logger;
        //}

        public MirrorExecutionService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<MirrorExecutionService> logger)
        {
            _scopeFactory = scopeFactory;
            _config = config;
            _logger = logger;
        }

        // ── Public entry point ────────────────────────────────────────────────
        public async Task<(bool Success, string? Error, int RowsAffected)> ExecuteAsync(
            int mirrorJobId, string triggeredBy = "Scheduler")
        {
            //await using var ctx = await _ctxFactory.CreateDbContextAsync();
            using var scope = _scopeFactory.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Load job with all relations
            var job = await ctx.MirrorJobs
                .Include(j => j.Source)
                .Include(j => j.Schedule)
                .Include(j => j.EmailConfig)
                .FirstOrDefaultAsync(j => j.Id == mirrorJobId);

            if (job == null)
                return (false, "Job not found.", 0);
            if (!job.IsActive)
                return (false, "Job is inactive.", 0);

            // Create history record (Running)
            var history = new JobHistory
            {
                MirrorJobId = job.Id,
                Status = JobStatus.Running,
                StartedAt = DateTime.Now,
                TriggeredBy = triggeredBy,
            };
            ctx.JobHistories.Add(history);
            await ctx.SaveChangesAsync();

            try
            {
                int rows = job.SyncMode == SyncMode.Incremental
                    ? await RunIncrementalAsync(ctx, job)
                    : await RunFullReplaceAsync(ctx, job);

                // Success
                history.Status = JobStatus.Success;
                history.FinishedAt = DateTime.Now;
                history.RowsAffected = rows;

                if (job.Schedule != null)
                {
                    job.Schedule.LastRunAt = DateTime.Now;
                    job.Schedule.NextRunAt = null; // Quartz will update this
                }

                await ctx.SaveChangesAsync();

                _logger.LogInformation("[DataBridge] Job {Name} completed: {Rows} rows.", job.Name, rows);
                return (true, null, rows);
            }
            catch (Exception ex)
            {
                history.Status = JobStatus.Failed;
                history.FinishedAt = DateTime.Now;
                history.RowsAffected = 0;
                history.ErrorMessage = ex.Message;
                await ctx.SaveChangesAsync();

                _logger.LogError(ex, "[DataBridge] Job {Name} failed.", job.Name);

                // Send email notification if configured
                if (job.EmailConfig?.NotifyOnFail == true)
                    await TrySendEmailAsync(job, false, ex.Message);

                return (false, ex.Message, 0);
            }
        }

        // ── Full Replace ──────────────────────────────────────────────────────
        /// Truncates destination table, then bulk-inserts all rows from source.
        private async Task<int> RunFullReplaceAsync(AppDbContext ctx, MirrorJob job)
        {
            var mirrorConnStr = _config.GetConnectionString("MirrorConnection")
                             ?? _config.GetConnectionString("DefaultConnection")!;

            // 1. Fetch from source
            var dt = await FetchFromSourceAsync(job.Source.ConnectionString, job.SourceQuery, null, null);

            // 2. Write to mirror
            await using var mirrorConn = new SqlConnection(mirrorConnStr);
            await mirrorConn.OpenAsync();

            // Truncate
            await using (var cmd = new SqlCommand($"TRUNCATE TABLE {job.DestinationTable}", mirrorConn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Bulk insert
            if (dt.Rows.Count > 0)
            {
                using var bulk = new SqlBulkCopy(mirrorConn)
                {
                    DestinationTableName = job.DestinationTable,
                    BulkCopyTimeout = 300,
                };
                // Map columns by name
                foreach (DataColumn col in dt.Columns)
                    bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);

                await bulk.WriteToServerAsync(dt);
            }

            return dt.Rows.Count;
        }

        // ── Incremental ───────────────────────────────────────────────────────
        /// Fetches only rows newer than LastWatermark, upserts into destination.
        private async Task<int> RunIncrementalAsync(AppDbContext ctx, MirrorJob job)
        {
            if (string.IsNullOrWhiteSpace(job.WatermarkColumn))
                throw new InvalidOperationException("WatermarkColumn is required for Incremental mode.");

            var mirrorConnStr = _config.GetConnectionString("MirrorConnection")
                             ?? _config.GetConnectionString("DefaultConnection")!;

            // 1. Fetch from source (filtered by watermark)
            var dt = await FetchFromSourceAsync(
                job.Source.ConnectionString,
                job.SourceQuery,
                job.WatermarkColumn,
                job.LastWatermark);

            if (dt.Rows.Count == 0) return 0;

            // 2. Get first column as merge key (assume it's the PK)
            string pkColumn = dt.Columns[0].ColumnName;

            // 3. Write to mirror via MERGE
            await using var mirrorConn = new SqlConnection(mirrorConnStr);
            await mirrorConn.OpenAsync();

            // Create temp table
            string tempTable = $"#tmp_{Guid.NewGuid():N}";
            await CreateTempTableAsync(mirrorConn, dt, tempTable);

            // Bulk insert into temp
            using (var bulk = new SqlBulkCopy(mirrorConn) { DestinationTableName = tempTable, BulkCopyTimeout = 300 })
            {
                foreach (DataColumn col in dt.Columns)
                    bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                await bulk.WriteToServerAsync(dt);
            }

            // MERGE
            string mergeSql = BuildMergeSql(job.DestinationTable, tempTable, pkColumn, dt.Columns);
            await using (var cmd = new SqlCommand(mergeSql, mirrorConn) { CommandTimeout = 300 })
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Update watermark
            var maxWatermark = dt.AsEnumerable()
                .Max(r => r[job.WatermarkColumn] as DateTime?);
            if (maxWatermark.HasValue)
                job.LastWatermark = maxWatermark;

            return dt.Rows.Count;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static async Task<DataTable> FetchFromSourceAsync(
            string connStr, string query, string? watermarkColumn, DateTime? since)
        {
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            string sql = query;
            if (!string.IsNullOrWhiteSpace(watermarkColumn) && since.HasValue)
            {
                // Wrap user query to add watermark filter
                sql = $"SELECT * FROM ({query}) AS __src__ WHERE [{watermarkColumn}] > @since";
            }

            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 300 };
            if (!string.IsNullOrWhiteSpace(watermarkColumn) && since.HasValue)
                cmd.Parameters.AddWithValue("@since", since.Value);

            var dt = new DataTable();
            using var adapter = new SqlDataAdapter(cmd);
            adapter.Fill(dt);
            return dt;
        }

        private static async Task CreateTempTableAsync(SqlConnection conn, DataTable dt, string tempTable)
        {
            var cols = string.Join(", ", dt.Columns.Cast<DataColumn>()
                .Select(c => $"[{c.ColumnName}] NVARCHAR(MAX)"));
            string sql = $"CREATE TABLE {tempTable} ({cols})";
            await using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private static string BuildMergeSql(
            string destTable, string tempTable, string pkCol, DataColumnCollection columns)
        {
            var allCols = columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            var updateCols = allCols.Where(c => c != pkCol)
                .Select(c => $"target.[{c}] = source.[{c}]");
            var insertCols = string.Join(", ", allCols.Select(c => $"[{c}]"));
            var insertVals = string.Join(", ", allCols.Select(c => $"source.[{c}]"));

            return $@"
MERGE {destTable} AS target
USING {tempTable} AS source
ON target.[{pkCol}] = source.[{pkCol}]
WHEN MATCHED THEN UPDATE SET {string.Join(", ", updateCols)}
WHEN NOT MATCHED BY TARGET THEN INSERT ({insertCols}) VALUES ({insertVals});";
        }

        private async Task TrySendEmailAsync(MirrorJob job, bool success, string? errorMsg)
        {
            try
            {
                var cfg = job.EmailConfig!;
                using var client = new MailKit.Net.Smtp.SmtpClient();
                await client.ConnectAsync(cfg.SmtpHost, cfg.SmtpPort, MailKit.Security.SecureSocketOptions.Auto);
                if (!string.IsNullOrWhiteSpace(cfg.SmtpUser))
                    await client.AuthenticateAsync(cfg.SmtpUser, cfg.SmtpPassword);

                var msg = new MimeKit.MimeMessage();
                msg.From.Add(new MimeKit.MailboxAddress(cfg.SenderName, cfg.SenderEmail));
                foreach (var r in cfg.Recipients.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    msg.To.Add(MimeKit.MailboxAddress.Parse(r.Trim()));

                msg.Subject = $"[DataBridge] Job '{job.Name}' {(success ? "Succeeded" : "FAILED")}";
                msg.Body = new MimeKit.TextPart("plain")
                {
                    Text = success
                        ? $"Mirror job '{job.Name}' completed successfully at {DateTime.Now}."
                        : $"Mirror job '{job.Name}' FAILED at {DateTime.Now}.\n\nError:\n{errorMsg}"
                };

                await client.SendAsync(msg);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[DataBridge] Failed to send email notification for job {Name}.", job.Name);
            }
        }
    }
}