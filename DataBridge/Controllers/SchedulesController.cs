using DataBridge.Models.ViewModels.Schedules;
using DataBridge.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataBridge.Controllers
{
    public class SchedulesController : Controller
    {
        private readonly ScheduleService _svc;

        public SchedulesController(ScheduleService svc) => _svc = svc;

        private void SetBreadcrumb(params (string Label, string Url)[] extra)
        {
            var list = new List<(string, string)> { ("Mirroring", "#") };
            list.AddRange(extra);
            ViewData["Breadcrumbs"] = list;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Schedules";
            SetBreadcrumb();
            return View(await _svc.GetListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> Configure(int jobId)
        {
            ViewData["Title"] = "Configure Schedule";
            SetBreadcrumb(("Schedules", "/Schedules"));
            var vm = await _svc.GetForJobAsync(jobId);
            if (vm == null) return NotFound();
            vm.ParseCronExpression(); // populate UI fields from stored expression
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Configure(ScheduleFormViewModel vm)
        {
            ViewData["Title"] = "Configure Schedule";
            SetBreadcrumb(("Schedules", "/Schedules"));

            // Build the CronExpression from UI fields before validation
            vm.BuildCronExpression();

            if (!ModelState.IsValid) return View(vm);

            var (ok, err) = await _svc.SaveAsync(vm);
            if (!ok) { ModelState.AddModelError("", err!); return View(vm); }
            TempData["Success"] = "Schedule saved.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var (ok, err) = await _svc.ToggleAsync(id);
            return Json(new { success = ok, error = err });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (ok, err) = await _svc.DeleteAsync(id);
            if (ok) TempData["Success"] = "Schedule removed.";
            else TempData["Error"] = err;
            return RedirectToAction(nameof(Index));
        }
    }
}