namespace DataBridge.Models.Entities
{
    public class Source
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<MirrorJob> MirrorJobs { get; set; } = [];
    }
}