using DataBridge.Models.ViewModels.Sources;
using DataBridge.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataBridge.Controllers
{
    public class SourcesController : Controller
    {
        private readonly SourceService _svc;
        public SourcesController(SourceService svc) => _svc = svc;

        private void SetBreadcrumb(params (string Label, string Url)[] extra)
        {
            var list = new List<(string, string)> { ("Config", "#") };
            list.AddRange(extra);
            ViewData["Breadcrumbs"] = list;
        }

        public async Task<IActionResult> Index(string? search)
        {
            ViewData["Title"] = "Sources";
            SetBreadcrumb();
            var vm = await _svc.GetListAsync(search);
            return View(vm);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "New Source";
            SetBreadcrumb(("Sources", "/Sources"));
            return View(new SourceFormViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SourceFormViewModel vm)
        {
            ViewData["Title"] = "New Source";
            SetBreadcrumb(("Sources", "/Sources"));
            if (!ModelState.IsValid) return View(vm);
            var (ok, err) = await _svc.CreateAsync(vm);
            if (!ok) { ModelState.AddModelError("", err!); return View(vm); }
            TempData["Success"] = $"Source '{vm.Name}' created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Source";
            SetBreadcrumb(("Sources", "/Sources"));
            var vm = await _svc.GetForEditAsync(id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SourceFormViewModel vm)
        {
            ViewData["Title"] = "Edit Source";
            SetBreadcrumb(("Sources", "/Sources"));
            if (!ModelState.IsValid) return View(vm);
            var (ok, err) = await _svc.UpdateAsync(vm);
            if (!ok) { ModelState.AddModelError("", err!); return View(vm); }
            TempData["Success"] = "Source updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (ok, err) = await _svc.DeleteAsync(id);
            if (ok) TempData["Success"] = "Source deleted.";
            else TempData["Error"] = err;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var (ok, err) = await _svc.ToggleActiveAsync(id);
            return Json(new { success = ok, error = err });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult TestConnection([FromBody] TestConnectionRequest req)
        {
            var (ok, msg) = _svc.TestConnection(req.ConnectionString);
            return Json(new { success = ok, message = msg });
        }

        public class TestConnectionRequest { public string ConnectionString { get; set; } = ""; }
    }
}