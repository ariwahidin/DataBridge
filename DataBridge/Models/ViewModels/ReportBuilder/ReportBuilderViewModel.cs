using DataBridge.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace DataBridge.Models.ViewModels.ReportBuilder
{
    // ── List item ─────────────────────────────────────────────────────────────
    public class ReportListItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public int ParamCount { get; set; }
    }

    // ── Create / Edit report ──────────────────────────────────────────────────
    public class ReportFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Report name is required.")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "SQL Query is required.")]
        public string SqlQuery { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public List<ReportParamFormViewModel> Parameters { get; set; } = new();
    }

    // ── Parameter form row ────────────────────────────────────────────────────
    public class ReportParamFormViewModel
    {
        public int Id { get; set; }

        [Required]
        public string ParamName { get; set; } = string.Empty;

        [Required]
        public string Label { get; set; } = string.Empty;

        public ReportParamType ParamType { get; set; } = ReportParamType.Text;
        public bool IsRequired { get; set; } = true;
        public string? DefaultValue { get; set; }
        public string? DropdownSource { get; set; }
        public int SortOrder { get; set; }
    }

    // ── Run report (end-user) ─────────────────────────────────────────────────
    public class ReportRunViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<ReportParamRunViewModel> Parameters { get; set; } = new();
    }

    public class ReportParamRunViewModel
    {
        public int Id { get; set; }
        public string ParamName { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public ReportParamType ParamType { get; set; }
        public bool IsRequired { get; set; }
        public string? DefaultValue { get; set; }
        public string? DropdownSource { get; set; }

        // Filled when running
        public string? Value { get; set; }
        public string? ValueTo { get; set; }  // untuk DateRange

        // Populated for Dropdown
        public List<string> DropdownOptions { get; set; } = new();
    }

    // ── Query result ──────────────────────────────────────────────────────────
    public class ReportDataResult
    {
        public List<string> Columns { get; set; } = new();
        public List<Dictionary<string, string?>> Rows { get; set; } = new();
        public int TotalRows { get; set; }
        public bool Truncated { get; set; }     // true kalau > MaxRows
        public string? Error { get; set; }
    }
}