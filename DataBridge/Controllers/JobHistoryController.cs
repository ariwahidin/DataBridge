using DataBridge.Filters;
using DataBridge.Models.Enums;
using DataBridge.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataBridge.Controllers
{
    [RequireRole(UserRole.Admin)]
    public class JobHistoryController : Controller
    {
        private readonly JobHistoryService _svc;
        public JobHistoryController(JobHistoryService svc) => _svc = svc;

        public async Task<IActionResult> Index(int? jobId, string? status)
        {
            ViewData["Title"] = "Job History";
            ViewData["Breadcrumbs"] = new List<(string, string)> { ("Mirroring", "#") };
            return View(await _svc.GetListAsync(jobId, status));
        }

        public async Task<IActionResult> Export(int? jobId, string? status)
        {
            var bytes = await _svc.ExportExcelAsync(jobId, status);
            var filename = $"JobHistory_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
        }
    }
}