using DataBridge.Models.Enums;

namespace DataBridge.Models.Entities
{
    public class JobHistory
    {
        public int Id { get; set; }
        public int MirrorJobId { get; set; }
        public JobStatus Status { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public int? RowsAffected { get; set; }
        public string? ErrorMessage { get; set; }
        public string TriggeredBy { get; set; } = "Scheduler";

        public MirrorJob MirrorJob { get; set; } = null!;
    }
}