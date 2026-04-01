using System.ComponentModel.DataAnnotations;

namespace DataBridge.Models.ViewModels.Schedules
{
    public class ScheduleListViewModel
    {
        public List<ScheduleRowViewModel> Schedules { get; set; } = [];
    }

    public class ScheduleRowViewModel
    {
        public int Id { get; set; }
        public int MirrorJobId { get; set; }
        public string JobName { get; set; } = string.Empty;
        public string SourceName { get; set; } = string.Empty;
        public string CronExpression { get; set; } = string.Empty;
        public string CronLabel { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public DateTime? LastRunAt { get; set; }
        public DateTime? NextRunAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ScheduleFormViewModel
    {
        public int Id { get; set; }
        public int MirrorJobId { get; set; }
        public string JobName { get; set; } = string.Empty;

        /// <summary>
        /// Type selector: every_minute | every_hour | daily | weekly | weekday | custom
        /// </summary>
        public string ScheduleType { get; set; } = "every_minute";

        // every N minutes (1–59)
        [Range(1, 59)] public int EveryMinutes { get; set; } = 5;

        // every N hours (1–23)
        [Range(1, 23)] public int EveryHours { get; set; } = 1;

        // daily at HH:MM
        public string DailyTime { get; set; } = "08:00";

        // weekly — day 0=Sun..6=Sat, time HH:MM
        [Range(0, 6)] public int WeeklyDay { get; set; } = 1;
        public string WeeklyTime { get; set; } = "08:00";

        // weekday (Mon–Fri) at HH:MM
        public string WeekdayTime { get; set; } = "08:00";

        // raw Quartz 6-field cron
        public string CustomCron { get; set; } = "0 */5 * * * ?";

        // ── Stored expression (computed on POST before save) ──
        public string CronExpression { get; set; } = "every:5:minute";

        public bool IsEnabled { get; set; } = true;

        // ── Helper: build CronExpression from UI fields ──
        public void BuildCronExpression()
        {
            CronExpression = ScheduleType switch
            {
                "every_minute" => $"every:{EveryMinutes}:minute",
                "every_hour" => $"every:{EveryHours}:hour",
                "daily" => $"daily:{ParseTime(DailyTime)}",
                "weekly" => $"weekly:{WeeklyDay}:{ParseTime(WeeklyTime)}",
                "weekday" => $"weekday:{ParseTime(WeekdayTime)}",
                "custom" => CustomCron.Trim(),
                _ => $"every:{EveryMinutes}:minute",
            };
        }

        // ── Helper: populate UI fields from stored CronExpression ──
        public void ParseCronExpression()
        {
            if (string.IsNullOrWhiteSpace(CronExpression)) return;
            var p = CronExpression.Split(':');
            if (p[0] == "every" && p.Length == 3 && p[2] == "minute")
            {
                ScheduleType = "every_minute";
                EveryMinutes = int.TryParse(p[1], out var m) ? m : 5;
            }
            else if (p[0] == "every" && p.Length == 3 && p[2] == "hour")
            {
                ScheduleType = "every_hour";
                EveryHours = int.TryParse(p[1], out var h) ? h : 1;
            }
            else if (p[0] == "daily" && p.Length == 3)
            {
                ScheduleType = "daily";
                DailyTime = $"{p[1].PadLeft(2, '0')}:{p[2].PadLeft(2, '0')}";
            }
            else if (p[0] == "weekly" && p.Length == 4)
            {
                ScheduleType = "weekly";
                WeeklyDay = int.TryParse(p[1], out var d) ? d : 1;
                WeeklyTime = $"{p[2].PadLeft(2, '0')}:{p[3].PadLeft(2, '0')}";
            }
            else if (p[0] == "weekday" && p.Length == 3)
            {
                ScheduleType = "weekday";
                WeekdayTime = $"{p[1].PadLeft(2, '0')}:{p[2].PadLeft(2, '0')}";
            }
            else
            {
                ScheduleType = "custom";
                CustomCron = CronExpression;
            }
        }

        private static string ParseTime(string hhmm)
        {
            var parts = hhmm.Split(':');
            return parts.Length == 2 ? $"{parts[0]}:{parts[1]}" : "8:0";
        }
    }
}