namespace DataBridge.Models.Entities
{
    public enum ReportParamType
    {
        Text, Number, Date, DateRange, Dropdown
    }

    public class ReportParameter
    {
        public int Id { get; set; }
        public int ReportDefinitionId { get; set; }
        public string ParamName { get; set; } = string.Empty;  // nama di SQL: {{ParamName}}
        public string Label { get; set; } = string.Empty;  // label di UI
        public ReportParamType ParamType { get; set; } = ReportParamType.Text;
        public bool IsRequired { get; set; } = true;
        public string? DefaultValue { get; set; }
        public int SortOrder { get; set; }

        // Untuk Dropdown: query SQL atau JSON array ["A","B"]
        public string? DropdownSource { get; set; }

        public ReportDefinition? ReportDefinition { get; set; }
    }
}