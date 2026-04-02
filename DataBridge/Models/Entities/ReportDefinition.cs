namespace DataBridge.Models.Entities
{
    public class ReportDefinition
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string SqlQuery { get; set; } = string.Empty;   // bisa pakai {{ParamName}}
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }

        public ICollection<ReportParameter> Parameters { get; set; } = new List<ReportParameter>();
    }
}