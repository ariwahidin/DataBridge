namespace DataBridge.Models.Entities
{
    public class ActivityLog
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string? FullName { get; set; }
        public string? Role { get; set; }
        public string Module { get; set; } = string.Empty;  // e.g. "ReportBuilder"
        public string Action { get; set; } = string.Empty;  // e.g. "Create"
        public string? Description { get; set; }                  // e.g. "Created report 'Monthly Sales'"
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}