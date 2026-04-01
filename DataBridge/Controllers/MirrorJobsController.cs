using DataBridge.Models.ViewModels.MirrorJobs;
using DataBridge.Repositories.Interfaces;
using DataBridge.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataBridge.Controllers
{
    public class MirrorJobsController : Controller
    {
        private readonly MirrorJobService _svc;
        private readonly ISourceRepository _sourceRepo;

        public MirrorJobsController(MirrorJobService svc, ISourceRepository sourceRepo)
        {
            _svc = svc;
            _sourceRepo = sourceRepo;
        }

        private void SetBreadcrumb(params (string Label, string Url)[] extra)
        {
            var list = new List<(string, string)> { ("Mirroring", "#") };
            list.AddRange(extra);
            ViewData["Breadcrumbs"] = list;
        }

        public async Task<IActionResult> Index(string? search)
        {
            ViewData["Title"] = "Mirror Jobs";
            SetBreadcrumb();
            return View(await _svc.GetListAsync(search));
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "New Mirror Job";
            SetBreadcrumb(("Mirror Jobs", "/MirrorJobs"));
            return View(await _svc.GetEmptyFormAsync());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MirrorJobFormViewModel vm)
        {
            ViewData["Title"] = "New Mirror Job";
            SetBreadcrumb(("Mirror Jobs", "/MirrorJobs"));
            if (!ModelState.IsValid) { vm = await RepopulateAsync(vm); return View(vm); }
            var (ok, err) = await _svc.CreateAsync(vm);
            if (!ok) { ModelState.AddModelError("", err!); vm = await RepopulateAsync(vm); return View(vm); }
            TempData["Success"] = $"Mirror job '{vm.Name}' created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Mirror Job";
            SetBreadcrumb(("Mirror Jobs", "/MirrorJobs"));
            var vm = await _svc.GetForEditAsync(id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MirrorJobFormViewModel vm)
        {
            ViewData["Title"] = "Edit Mirror Job";
            SetBreadcrumb(("Mirror Jobs", "/MirrorJobs"));
            if (!ModelState.IsValid) { vm = await RepopulateAsync(vm); return View(vm); }
            var (ok, err) = await _svc.UpdateAsync(vm);
            if (!ok) { ModelState.AddModelError("", err!); vm = await RepopulateAsync(vm); return View(vm); }
            TempData["Success"] = "Mirror job updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (ok, err) = await _svc.DeleteAsync(id);
            if (ok) TempData["Success"] = "Job deleted.";
            else TempData["Error"] = err;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var (ok, err) = await _svc.ToggleActiveAsync(id);
            return Json(new { success = ok, error = err });
        }

        // ── NEW: Manual trigger ─────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> TriggerRun(int id)
        {
            var (ok, err) = await _svc.TriggerRunAsync(id);
            return Json(new { success = ok, error = err });
        }

        // ── NEW: Test source query ──────────────────────────────────────────────
        /// <summary>
        /// Validates + test-executes a SQL query against a given source.
        /// Body: { sourceId: int, query: string }
        /// </summary>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> TestQuery([FromBody] TestQueryRequest req)
        {
            if (req.SourceId <= 0 || string.IsNullOrWhiteSpace(req.Query))
                return Json(new { success = false, error = "sourceId and query are required." });

            // Static safety check first (no DB round-trip needed).
            var safetyError = MirrorJobService.CheckQuerySafety(req.Query);
            if (safetyError != null)
                return Json(new { success = false, error = safetyError });

            // Load connection string.
            var source = await _sourceRepo.GetByIdAsync(req.SourceId);
            if (source == null)
                return Json(new { success = false, error = "Source not found." });

            var (ok, err, colCount) = await _svc.ValidateSourceQueryAsync(source.ConnectionString, req.Query);
            return Json(new { success = ok, error = err, columnCount = colCount });
        }

        // ── Helpers ─────────────────────────────────────────────────────────────
        private async Task<MirrorJobFormViewModel> RepopulateAsync(MirrorJobFormViewModel vm)
        {
            var fresh = await _svc.GetEmptyFormAsync();
            vm.AvailableSources = fresh.AvailableSources;
            return vm;
        }

        public class TestQueryRequest
        {
            public int SourceId { get; set; }
            public string Query { get; set; } = string.Empty;
        }
    }
}