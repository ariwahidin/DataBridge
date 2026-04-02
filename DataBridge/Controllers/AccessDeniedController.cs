using Microsoft.AspNetCore.Mvc;

namespace DataBridge.Controllers
{
    public class AccessDeniedController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Access Denied";
            return View();
        }
    }
}