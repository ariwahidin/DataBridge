using DataBridge.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace DataBridge.Models.ViewModels.MirrorJobs
{
    public class MirrorJobListViewModel
    {
        public List<MirrorJobRowViewModel> Jobs { get; set; } = [];
        public string? Search { get; set; }
    }

    public class MirrorJobRowViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SourceName { get; set; } = string.Empty;
        public string DestinationTable { get; set; } = string.Empty;
        public SyncMode SyncMode { get; set; }
        public bool IsActive { get; set; }
        public bool HasSchedule { get; set; }
        public bool ScheduleEnabled { get; set; }
        public string? CronExpression { get; set; }
        public string? CronLabel { get; set; }          // ← NEW
        public DateTime? LastRunAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MirrorJobFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Job name is required")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Source is required")]
        public int SourceId { get; set; }

        [Required(ErrorMessage = "Source query is required")]
        public string SourceQuery { get; set; } = string.Empty;

        [Required(ErrorMessage = "Destination table is required")]
        [StringLength(200)]
        public string DestinationTable { get; set; } = string.Empty;

        public SyncMode SyncMode { get; set; } = SyncMode.FullReplace;

        [StringLength(100)]
        public string? WatermarkColumn { get; set; }

        public bool IsActive { get; set; } = true;

        public List<SourceOption> AvailableSources { get; set; } = [];
    }

    public class SourceOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}