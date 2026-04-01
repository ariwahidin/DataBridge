using DataBridge.Models.Enums;

namespace DataBridge.Models.Entities
{
    public class MirrorJob
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int SourceId { get; set; }
        public string SourceQuery { get; set; } = string.Empty;
        public string DestinationTable { get; set; } = string.Empty;
        public SyncMode SyncMode { get; set; } = SyncMode.FullReplace;
        public string? WatermarkColumn { get; set; }
        public DateTime? LastWatermark { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public Source Source { get; set; } = null!;
        public JobSchedule? Schedule { get; set; }
        public EmailConfig? EmailConfig { get; set; }
        public ICollection<JobHistory> Histories { get; set; } = [];
    }
}