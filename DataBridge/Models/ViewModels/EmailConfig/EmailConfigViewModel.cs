using System.ComponentModel.DataAnnotations;

namespace DataBridge.Models.ViewModels.EmailConfig
{
    public class EmailConfigListViewModel
    {
        public List<EmailConfigRowViewModel> Configs { get; set; } = [];
    }

    public class EmailConfigRowViewModel
    {
        public int Id { get; set; }
        public int MirrorJobId { get; set; }
        public string JobName { get; set; } = string.Empty;
        public string Recipients { get; set; } = string.Empty;
        public bool NotifyOnFail { get; set; }
        public bool NotifyOnSuccess { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class EmailConfigFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Mirror job is required")]
        public int MirrorJobId { get; set; }

        [Required(ErrorMessage = "At least one recipient is required")]
        public string Recipients { get; set; } = string.Empty;

        public bool NotifyOnFail { get; set; } = true;
        public bool NotifyOnSuccess { get; set; } = false;

        [Required(ErrorMessage = "SMTP host is required")]
        [StringLength(200)]
        public string SmtpHost { get; set; } = string.Empty;

        [Range(1, 65535, ErrorMessage = "Invalid port")]
        public int SmtpPort { get; set; } = 587;

        [StringLength(200)]
        public string SmtpUser { get; set; } = string.Empty;

        public string SmtpPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sender email is required")]
        [EmailAddress]
        [StringLength(200)]
        public string SenderEmail { get; set; } = string.Empty;

        [StringLength(100)]
        public string SenderName { get; set; } = "DataBridge";

        public string JobName { get; set; } = string.Empty;
        public List<JobOption> AvailableJobs { get; set; } = [];
    }

    public class JobOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}