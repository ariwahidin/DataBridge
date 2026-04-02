using DataBridge.Models.ViewModels.Auth;
using DataBridge.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataBridge.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthService _authService;
        private readonly ActivityLogService _logSvc;

        public AuthController(AuthService authService, ActivityLogService logSvc)
        {
            _authService = authService;
            _logSvc = logSvc;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (HttpContext.Session.GetString("UserId") != null)
                return RedirectToAction("Index", "Dashboard");
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);
            try
            {
                var user = await _authService.ValidateAsync(model.Username, model.Password);
                if (user == null)
                {
                    // Log failed login
                    await _logSvc.LogAsync("Auth", "Login",
                        username: model.Username,
                        description: $"Failed login attempt for '{model.Username}'",
                        isSuccess: false,
                        errorMessage: "Invalid username or password");

                    ModelState.AddModelError("", "Invalid username or password.");
                    return View(model);
                }

                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("FullName", user.FullName);
                HttpContext.Session.SetString("Role", user.Role.ToString());

                // Log successful login
                await _logSvc.LogAsync("Auth", "Login",
                    userId: user.Id,
                    username: user.Username,
                    fullName: user.FullName,
                    role: user.Role.ToString(),
                    description: $"User '{user.Username}' logged in");

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error: {ex.Message}");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var username = HttpContext.Session.GetString("Username");
            var userId = int.TryParse(HttpContext.Session.GetString("UserId"), out var uid) ? uid : (int?)null;
            var fullName = HttpContext.Session.GetString("FullName");
            var role = HttpContext.Session.GetString("Role");

            // Log dulu sebelum session di-clear
            await _logSvc.LogAsync("Auth", "Logout",
                userId: userId,
                username: username,
                fullName: fullName,
                role: role,
                description: $"User '{username}' logged out");

            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}