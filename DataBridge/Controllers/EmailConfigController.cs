using DataBridge.Filters;
using DataBridge.Models.Enums;
using DataBridge.Models.ViewModels.EmailConfig;
using DataBridge.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataBridge.Controllers
{
    [RequireRole(UserRole.Admin)]
    public class EmailConfigController : Controller
    {
        private readonly EmailConfigService _svc;
        public EmailConfigController(EmailConfigService svc) => _svc = svc;

        private void SetBreadcrumb(params (string Label, string Url)[] extra)
        {
            var list = new List<(string, string)> { ("Config", "#") };
            list.AddRange(extra);
            ViewData["Breadcrumbs"] = list;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Notifications";
            SetBreadcrumb();
            return View(await _svc.GetListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "New Notification Config";
            SetBreadcrumb(("Notifications", "/EmailConfig"));
            return View(await _svc.GetEmptyFormAsync());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmailConfigFormViewModel vm)
        {
            ViewData["Title"] = "New Notification Config";
            SetBreadcrumb(("Notifications", "/EmailConfig"));
            if (!ModelState.IsValid) { vm = await Repopulate(vm); return View(vm); }
            var (ok, err) = await _svc.CreateAsync(vm);
            if (!ok) { ModelState.AddModelError("", err!); vm = await Repopulate(vm); return View(vm); }
            TempData["Success"] = "Notification config created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Notification Config";
            SetBreadcrumb(("Notifications", "/EmailConfig"));
            var vm = await _svc.GetForEditAsync(id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EmailConfigFormViewModel vm)
        {
            ViewData["Title"] = "Edit Notification Config";
            SetBreadcrumb(("Notifications", "/EmailConfig"));
            if (!ModelState.IsValid) { vm = await Repopulate(vm); return View(vm); }
            var (ok, err) = await _svc.UpdateAsync(vm);
            if (!ok) { ModelState.AddModelError("", err!); vm = await Repopulate(vm); return View(vm); }
            TempData["Success"] = "Notification config updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (ok, err) = await _svc.DeleteAsync(id);
            if (ok) TempData["Success"] = "Config deleted.";
            else TempData["Error"] = err;
            return RedirectToAction(nameof(Index));
        }

        private async Task<EmailConfigFormViewModel> Repopulate(EmailConfigFormViewModel vm)
        {
            var fresh = await _svc.GetEmptyFormAsync();
            vm.AvailableJobs = fresh.AvailableJobs;
            return vm;
        }
    }
}