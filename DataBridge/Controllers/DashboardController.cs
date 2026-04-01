using Microsoft.AspNetCore.Mvc;

namespace DataBridge.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
