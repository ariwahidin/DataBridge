using DataBridge.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataBridge.Controllers
{
    public class ReportViewerController : Controller
    {
        private readonly ReportBuilderService _svc;

        public ReportViewerController(ReportBuilderService svc) => _svc = svc;

        private void SetBreadcrumb(params (string Label, string Url)[] extra)
        {
            var list = new List<(string, string)> { ("Reports", "#") };
            list.AddRange(extra);
            ViewData["Breadcrumbs"] = list;
        }

        // ── List active reports ───────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Reports";
            SetBreadcrumb(("Reports", "/ReportViewer"));
            var list = await _svc.GetActiveAsync();
            return View(list);
        }

        // ── Report page ───────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Run(int id)
        {
            var vm = await _svc.GetRunVmAsync(id);
            if (vm == null) { TempData["Error"] = "Report not found."; return RedirectToAction(nameof(Index)); }

            ViewData["Title"] = vm.Name;
            SetBreadcrumb(("Reports", "/ReportViewer"), (vm.Name, "#"));
            return View(vm);
        }

        // ── Execute report (AJAX) ─────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Execute([FromBody] ExecuteRequest req)
        {
            var result = await _svc.ExecuteAsync(req.ReportId, req.Values);
            return Json(result);
        }

        // ── Download Excel ────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> DownloadExcel(int id, [FromQuery] Dictionary<string, string> values)
        {
            try
            {
                var bytes = await _svc.ExportExcelAsync(id, values.ToDictionary(k => k.Key, v => (string?)v.Value));
                var report = await _svc.GetRunVmAsync(id);
                var fileName = $"{report?.Name ?? "Report"}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Run), new { id });
            }
        }
    }

    public class ExecuteRequest
    {
        public int ReportId { get; set; }
        public Dictionary<string, string?> Values { get; set; } = new();
    }
}