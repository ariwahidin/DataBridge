using DataBridge.Filters;
using DataBridge.Models.Enums;
using DataBridge.Models.ViewModels.ReportBuilder;
using DataBridge.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace DataBridge.Controllers
{
    [RequireRole(UserRole.Admin)]
    public class ReportBuilderController : Controller
    {
        private readonly ReportBuilderService _svc;

        public ReportBuilderController(ReportBuilderService svc) => _svc = svc;

        private void SetBreadcrumb(params (string Label, string Url)[] extra)
        {
            var list = new List<(string, string)> { ("Reports", "#") };
            list.AddRange(extra);
            ViewData["Breadcrumbs"] = list;
        }

        // ── List ──────────────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Report Builder";
            SetBreadcrumb(("Report Builder", "/ReportBuilder"));
            var list = await _svc.GetAllAsync();
            return View(list);
        }

        // ── Create ────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "New Report";
            SetBreadcrumb(("Report Builder", "/ReportBuilder"), ("New Report", "#"));
            return View(new ReportFormViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        [LogActivity("ReportBuilder", "Create")]
        public async Task<IActionResult> Create(ReportFormViewModel vm)
        {
            ViewData["Title"] = "New Report";
            SetBreadcrumb(("Report Builder", "/ReportBuilder"), ("New Report", "#"));
            if (!ModelState.IsValid) return View(vm);

            var (ok, err) = await _svc.SaveAsync(vm, User.Identity?.Name ?? "admin");
            if (!ok) { ModelState.AddModelError("", err!); return View(vm); }

            TempData["Success"] = $"Report '{vm.Name}' created.";
            return RedirectToAction(nameof(Index));
        }

        // ── Edit ──────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Report";
            SetBreadcrumb(("Report Builder", "/ReportBuilder"), ("Edit", "#"));
            var vm = await _svc.GetFormAsync(id);
            if (vm == null) { TempData["Error"] = "Report not found."; return RedirectToAction(nameof(Index)); }
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [LogActivity("ReportBuilder", "Edit")]
        public async Task<IActionResult> Edit(ReportFormViewModel vm)
        {
            ViewData["Title"] = "Edit Report";
            SetBreadcrumb(("Report Builder", "/ReportBuilder"), ("Edit", "#"));
            if (!ModelState.IsValid) return View(vm);

            var (ok, err) = await _svc.SaveAsync(vm, User.Identity?.Name ?? "admin");
            if (!ok) { ModelState.AddModelError("", err!); return View(vm); }

            TempData["Success"] = "Report updated.";
            return RedirectToAction(nameof(Index));
        }

        // ── Delete ────────────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        [LogActivity("ReportBuilder", "Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var (ok, err) = await _svc.DeleteAsync(id);
            TempData[ok ? "Success" : "Error"] = ok ? "Report deleted." : err;
            return RedirectToAction(nameof(Index));
        }

        // ── Preview query (AJAX) ──────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Preview([FromBody] PreviewRequest req)
        {
            var result = await _svc.PreviewAsync(req.Sql, req.Params, req.Values);
            return Json(result);
        }
    }

    public class PreviewRequest
    {
        public string Sql { get; set; } = string.Empty;
        public List<DataBridge.Models.ViewModels.ReportBuilder.ReportParamFormViewModel> Params { get; set; } = new();
        public Dictionary<string, string?> Values { get; set; } = new();
    }
}