using DataBridge.Data;
using DataBridge.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataBridge.Services
{
    public class ActivityLogService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ActivityLogService> _logger;

        public ActivityLogService(
            IServiceScopeFactory scopeFactory,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ActivityLogService> logger)
        {
            _scopeFactory = scopeFactory;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // ── Fire and forget — tidak block request ─────────────────────────────
        public void Log(
            string module,
            string action,
            string? description = null,
            bool isSuccess = true,
            string? errorMessage = null)
        {
            var ctx = _httpContextAccessor.HttpContext;

            var entry = new ActivityLog
            {
                UserId = int.TryParse(ctx?.Session.GetString("UserId"), out var uid) ? uid : null,
                Username = ctx?.Session.GetString("Username"),
                FullName = ctx?.Session.GetString("FullName"),
                Role = ctx?.Session.GetString("Role"),
                Module = module,
                Action = action,
                Description = description,
                IpAddress = GetIp(ctx),
                UserAgent = ctx?.Request.Headers["User-Agent"].ToString().Truncate(500),
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                Timestamp = DateTime.Now,
            };

            // Fire and forget — pakai background thread supaya tidak block response
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.ActivityLogs.Add(entry);
                    await db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to write activity log");
                }
            });
        }

        // ── Versi async untuk AuthController (Login/Logout) ───────────────────
        public async Task LogAsync(
            string module,
            string action,
            int? userId = null,
            string? username = null,
            string? fullName = null,
            string? role = null,
            string? description = null,
            bool isSuccess = true,
            string? errorMessage = null)
        {
            var ctx = _httpContextAccessor.HttpContext;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userId,
                    Username = username,
                    FullName = fullName,
                    Role = role,
                    Module = module,
                    Action = action,
                    Description = description,
                    IpAddress = GetIp(ctx),
                    UserAgent = ctx?.Request.Headers["User-Agent"].ToString().Truncate(500),
                    IsSuccess = isSuccess,
                    ErrorMessage = errorMessage,
                    Timestamp = DateTime.Now,
                });
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write activity log");
            }
        }

        // ── Query untuk viewer ────────────────────────────────────────────────
        public async Task<(List<ActivityLog> Items, int Total)> GetPagedAsync(
            int page = 1,
            int pageSize = 50,
            string? username = null,
            string? module = null,
            string? action = null,
            bool? isSuccess = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var q = db.ActivityLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(username))
                q = q.Where(x => x.Username!.Contains(username));
            if (!string.IsNullOrWhiteSpace(module))
                q = q.Where(x => x.Module == module);
            if (!string.IsNullOrWhiteSpace(action))
                q = q.Where(x => x.Action == action);
            if (isSuccess.HasValue)
                q = q.Where(x => x.IsSuccess == isSuccess.Value);
            if (dateFrom.HasValue)
                q = q.Where(x => x.Timestamp >= dateFrom.Value);
            if (dateTo.HasValue)
                q = q.Where(x => x.Timestamp < dateTo.Value.AddDays(1));

            var total = await q.CountAsync();
            var items = await q
                .OrderByDescending(x => x.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<List<string>> GetDistinctModulesAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await db.ActivityLogs
                .Select(x => x.Module)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();
        }

        // ── Helper ────────────────────────────────────────────────────────────
        private static string? GetIp(HttpContext? ctx)
        {
            if (ctx == null) return null;
            var forwarded = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwarded))
                return forwarded.Split(',')[0].Trim();
            return ctx.Connection.RemoteIpAddress?.ToString();
        }
    }

    // Extension helper
    internal static class StringExtensions
    {
        public static string Truncate(this string s, int max) =>
            s.Length <= max ? s : s[..max];
    }
}