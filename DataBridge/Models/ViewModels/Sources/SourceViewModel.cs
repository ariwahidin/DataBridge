using System.ComponentModel.DataAnnotations;

namespace DataBridge.Models.ViewModels.Sources
{
    public class SourceListViewModel
    {
        public List<SourceRowViewModel> Sources { get; set; } = [];
        public string? Search { get; set; }
    }

    public class SourceRowViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int JobCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class SourceFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, ErrorMessage = "Name max 200 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Connection string is required")]
        public string ConnectionString { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description max 500 characters")]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}