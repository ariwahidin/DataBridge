namespace DataBridge.Models.Entities
{
    public class EmailConfig
    {
        public int Id { get; set; }
        public int MirrorJobId { get; set; }
        public string Recipients { get; set; } = string.Empty;
        public bool NotifyOnFail { get; set; } = true;
        public bool NotifyOnSuccess { get; set; } = false;
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string SmtpUser { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = "DataBridge";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public MirrorJob MirrorJob { get; set; } = null!;
    }
}