using DataBridge.Filters;
using DataBridge.Models.Enums;
using DataBridge.Models.ViewModels.Users;
using DataBridge.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataBridge.Controllers
{
    [RequireRole(UserRole.Admin)]
    public class UsersController : Controller
    {
        private readonly UserService _userService;

        public UsersController(UserService userService)
        {
            _userService = userService;
        }

        // ─── Guard: Admin only ───────────────────────────────────────────
        private IActionResult? RequireAdmin()
        {
            if (HttpContext.Session.GetString("Role") != UserRole.Admin.ToString())
                return RedirectToAction("Index", "Dashboard");
            return null;
        }

        private int CurrentUserId()
            => int.TryParse(HttpContext.Session.GetString("UserId"), out var id) ? id : 0;

        // ─── LIST ────────────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? search, string? role)
        {
            var guard = RequireAdmin();
            if (guard != null) return guard;

            ViewData["Title"] = "Users";
            ViewData["Breadcrumbs"] = new List<(string, string)>
            {
                ("Config", "#")
            };

            var vm = await _userService.GetListAsync(search, role);
            return View(vm);
        }

        // ─── CREATE ──────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Create()
        {
            var guard = RequireAdmin();
            if (guard != null) return guard;

            ViewData["Title"] = "New User";
            ViewData["Breadcrumbs"] = new List<(string, string)>
            {
                ("Config", "#"), ("Users", "/Users")
            };

            return View(new UserCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel vm)
        {
            var guard = RequireAdmin();
            if (guard != null) return guard;

            ViewData["Title"] = "New User";
            ViewData["Breadcrumbs"] = new List<(string, string)>
            {
                ("Config", "#"), ("Users", "/Users")
            };

            if (!ModelState.IsValid) return View(vm);

            var (success, error) = await _userService.CreateAsync(vm);
            if (!success)
            {
                ModelState.AddModelError("", error!);
                return View(vm);
            }

            TempData["Success"] = $"User '{vm.Username}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ─── EDIT ────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var guard = RequireAdmin();
            if (guard != null) return guard;

            var vm = await _userService.GetForEditAsync(id);
            if (vm == null) return NotFound();

            ViewData["Title"] = $"Edit — {vm.Username}";
            ViewData["Breadcrumbs"] = new List<(string, string)>
            {
                ("Config", "#"), ("Users", "/Users")
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel vm)
        {
            var guard = RequireAdmin();
            if (guard != null) return guard;

            ViewData["Title"] = $"Edit — {vm.Username}";
            ViewData["Breadcrumbs"] = new List<(string, string)>
            {
                ("Config", "#"), ("Users", "/Users")
            };

            if (!ModelState.IsValid) return View(vm);

            var (success, error) = await _userService.UpdateAsync(vm, CurrentUserId());
            if (!success)
            {
                ModelState.AddModelError("", error!);
                return View(vm);
            }

            TempData["Success"] = "User updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ─── RESET PASSWORD ──────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var guard = RequireAdmin();
            if (guard != null) return guard;

            var vm = await _userService.GetForResetAsync(id);
            if (vm == null) return NotFound();

            ViewData["Title"] = $"Reset Password — {vm.Username}";
            ViewData["Breadcrumbs"] = new List<(string, string)>
            {
                ("Config", "#"), ("Users", "/Users")
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel vm)
        {
            var guard = RequireAdmin();
            if (guard != null) return guard;

            ViewData["Title"] = $"Reset Password — {vm.Username}";
            ViewData["Breadcrumbs"] = new List<(string, string)>
            {
                ("Config", "#"), ("Users", "/Users")
            };

            if (!ModelState.IsValid) return View(vm);

            var (success, error) = await _userService.ResetPasswordAsync(vm);
            if (!success)
            {
                ModelState.AddModelError("", error!);
                return View(vm);
            }

            TempData["Success"] = $"Password for '{vm.Username}' has been reset.";
            return RedirectToAction(nameof(Index));
        }

        // ─── TOGGLE ACTIVE (AJAX) ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var guard = RequireAdmin();
            if (guard != null) return Json(new { success = false, error = "Forbidden" });

            var (success, error) = await _userService.ToggleActiveAsync(id, CurrentUserId());
            return Json(new { success, error });
        }
    }
}
