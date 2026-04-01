// FILE: Services/QuartzSchedulerManager.cs  (REPLACE seluruh file)
using DataBridge.Data;
using DataBridge.Jobs;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace DataBridge.Services
{
    public class QuartzSchedulerManager
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly IServiceScopeFactory _scopeFactory;   // ← ganti dari IDbContextFactory
        private readonly ILogger<QuartzSchedulerManager> _logger;

        private const string GROUP = "DataBridge";

        public QuartzSchedulerManager(
            ISchedulerFactory schedulerFactory,
            IServiceScopeFactory scopeFactory,             // ← ganti
            ILogger<QuartzSchedulerManager> logger)
        {
            _schedulerFactory = schedulerFactory;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task SyncAllSchedulesAsync()
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            // Buat scope sendiri untuk akses DB dari singleton
            using var scope = _scopeFactory.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var schedules = await ctx.JobSchedules
                .Include(s => s.MirrorJob)
                .ToListAsync();

            foreach (var s in schedules)
            {
                if (s.IsEnabled && s.MirrorJob.IsActive)
                    await ScheduleJobAsync(scheduler, s.MirrorJobId, s.CronExpression);
                else
                    await UnscheduleJobAsync(scheduler, s.MirrorJobId);
            }
        }

        public async Task ScheduleJobAsync(int mirrorJobId, string cronExpression)
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            await ScheduleJobAsync(scheduler, mirrorJobId, cronExpression);
        }

        private async Task ScheduleJobAsync(IScheduler scheduler, int mirrorJobId, string cronExpression)
        {
            var jobKey = new JobKey($"mirror-{mirrorJobId}", GROUP);
            var triggerKey = new TriggerKey($"trigger-{mirrorJobId}", GROUP);

            string quartzCron = ToQuartzCron(cronExpression);

            var jobDetail = JobBuilder.Create<MirrorSyncJob>()
                .WithIdentity(jobKey)
                .UsingJobData("mirrorJobId", mirrorJobId)
                .StoreDurably()
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity(triggerKey)
                .ForJob(jobKey)
                .WithCronSchedule(quartzCron, x => x.WithMisfireHandlingInstructionDoNothing())
                .Build();

            if (await scheduler.CheckExists(jobKey))
                await scheduler.DeleteJob(jobKey);

            await scheduler.ScheduleJob(jobDetail, trigger);
            _logger.LogInformation("[DataBridge] Scheduled job {Id} → {Cron}", mirrorJobId, quartzCron);
        }

        public async Task UnscheduleJobAsync(int mirrorJobId)
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            await UnscheduleJobAsync(scheduler, mirrorJobId);
        }

        private async Task UnscheduleJobAsync(IScheduler scheduler, int mirrorJobId)
        {
            var jobKey = new JobKey($"mirror-{mirrorJobId}", GROUP);
            if (await scheduler.CheckExists(jobKey))
            {
                await scheduler.DeleteJob(jobKey);
                _logger.LogInformation("[DataBridge] Unscheduled job {Id}", mirrorJobId);
            }
        }

        public async Task TriggerNowAsync(int mirrorJobId)
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobKey = new JobKey($"mirror-{mirrorJobId}", GROUP);

            if (await scheduler.CheckExists(jobKey))
            {
                await scheduler.TriggerJob(jobKey);
            }
            else
            {
                var jobDetail = JobBuilder.Create<MirrorSyncJob>()
                    .WithIdentity(jobKey)
                    .UsingJobData("mirrorJobId", mirrorJobId)
                    .StoreDurably(false)
                    .Build();

                var trigger = TriggerBuilder.Create()
                    .ForJob(jobKey)
                    .StartNow()
                    .Build();

                await scheduler.ScheduleJob(jobDetail, trigger);
            }
        }

        // ── Cron helpers (tidak berubah) ─────────────────────────────────────
        public static string ToQuartzCron(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) return "0 0 * * * ?";
            var parts = expression.Split(':');

            if (parts.Length == 3 && parts[0] == "every" && parts[2] == "minute"
                && int.TryParse(parts[1], out int mins) && mins >= 1)
                return $"0 */{mins} * * * ?";

            if (parts.Length == 3 && parts[0] == "every" && parts[2] == "hour"
                && int.TryParse(parts[1], out int hrs) && hrs >= 1)
                return $"0 0 */{hrs} * * ?";

            if (parts.Length == 3 && parts[0] == "daily"
                && int.TryParse(parts[1], out int dh) && int.TryParse(parts[2], out int dm))
                return $"0 {dm} {dh} * * ?";

            if (parts.Length == 4 && parts[0] == "weekly"
                && int.TryParse(parts[1], out int dow)
                && int.TryParse(parts[2], out int wh)
                && int.TryParse(parts[3], out int wm))
            {
                int quartzDow = (dow % 7) + 1;
                return $"0 {wm} {wh} ? * {quartzDow}";
            }

            if (parts.Length == 3 && parts[0] == "weekday"
                && int.TryParse(parts[1], out int wd_h) && int.TryParse(parts[2], out int wd_m))
                return $"0 {wd_m} {wd_h} ? * MON-FRI";

            if (expression.Split(' ').Length == 6) return expression;

            return "0 0 * * * ?";
        }

        public static string ToLabel(string expression)
        {
            var parts = expression.Split(':');
            if (parts.Length == 3 && parts[0] == "every" && parts[2] == "minute")
                return $"Every {parts[1]} minute(s)";
            if (parts.Length == 3 && parts[0] == "every" && parts[2] == "hour")
                return $"Every {parts[1]} hour(s)";
            if (parts.Length == 3 && parts[0] == "daily")
                return $"Daily at {parts[1]}:{parts[2]}";
            if (parts.Length == 4 && parts[0] == "weekly")
            {
                var days = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
                int d = int.TryParse(parts[1], out int dv) ? dv : 0;
                return $"Weekly {days[d % 7]} at {parts[2]}:{parts[3]}";
            }
            if (parts.Length == 3 && parts[0] == "weekday")
                return $"Mon–Fri at {parts[1]}:{parts[2]}";
            return expression;
        }
    }
}