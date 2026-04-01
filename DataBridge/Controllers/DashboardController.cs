using DataBridge.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataBridge.Controllers
{
    public class DashboardController : Controller
    {
        private readonly DashboardService _svc;
        public DashboardController(DashboardService svc) => _svc = svc;

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Dashboard";
            var vm = await _svc.GetAsync();
            return View(vm);
        }
    }
}