namespace DataBridge.Models.Entities
{
    public class MirrorTableRegistry
    {
        public int Id { get; set; }
        public string Schema { get; set; } = "dbo";
        public string TableName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }

        // Computed helper (not mapped)
        public string FullName => $"{Schema}.{TableName}";
    }
}