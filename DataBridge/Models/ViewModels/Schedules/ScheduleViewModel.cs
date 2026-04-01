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

        [Required(ErrorMessage = "Cron expression is required")]
        [StringLength(100)]
        public string CronExpression { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;
    }
}