namespace DataBridge.Models.Entities
{
    public class JobSchedule
    {
        public int Id { get; set; }
        public int MirrorJobId { get; set; }
        public string CronExpression { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public DateTime? LastRunAt { get; set; }
        public DateTime? NextRunAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public MirrorJob MirrorJob { get; set; } = null!;
    }
}