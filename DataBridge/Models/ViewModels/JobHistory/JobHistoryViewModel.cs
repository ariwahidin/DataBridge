using DataBridge.Models.Enums;

namespace DataBridge.Models.ViewModels.JobHistory
{
    public class JobHistoryListViewModel
    {
        public List<JobHistoryRowViewModel> Histories { get; set; } = [];
        public int? FilterJobId { get; set; }
        public string? FilterStatus { get; set; }
        public string? FilterJobName { get; set; }
        public int TotalCount { get; set; }
    }

    public class JobHistoryRowViewModel
    {
        public int Id { get; set; }
        public int MirrorJobId { get; set; }
        public string JobName { get; set; } = string.Empty;
        public JobStatus Status { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public int? RowsAffected { get; set; }
        public string? ErrorMessage { get; set; }
        public string TriggeredBy { get; set; } = string.Empty;
        public double? DurationSeconds => FinishedAt.HasValue ? (FinishedAt.Value - StartedAt).TotalSeconds : null;
    }
}