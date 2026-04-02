using DataBridge.Filters;
using DataBridge.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataBridge.Controllers
{
    [RequireRole(DataBridge.Models.Enums.UserRole.Admin)]
    public class ActivityLogController : Controller
    {
        private readonly ActivityLogService _svc;

        public ActivityLogController(ActivityLogService svc) => _svc = svc;

        private void SetBreadcrumb(params (string Label, string Url)[] extra)
        {
            var list = new List<(string, string)> { ("Admin", "#") };
            list.AddRange(extra);
            ViewData["Breadcrumbs"] = list;
        }

        public async Task<IActionResult> Index(
            string? username = null,
            string? module = null,
            string? actionFilter = null,
            //string? action = null,
            bool? isSuccess = null,
            string? dateFrom = null,
            string? dateTo = null,
            int page = 1)
        {
            ViewData["Title"] = "Activity Log";
            SetBreadcrumb(("Activity Log", "/ActivityLog"));

            DateTime? from = DateTime.TryParse(dateFrom, out var df) ? df : null;
            DateTime? to = DateTime.TryParse(dateTo, out var dt) ? dt : null;

            var (items, total) = await _svc.GetPagedAsync(
                page, 50, username, module, actionFilter, isSuccess, from, to);

            var modules = await _svc.GetDistinctModulesAsync();

            ViewBag.Items = items;
            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.FilterAction = actionFilter;
            ViewBag.PageSize = 50;
            ViewBag.TotalPages = (int)Math.Ceiling(total / 50.0);
            ViewBag.Modules = modules;

            // Preserve filters
            ViewBag.FilterUsername = username;
            ViewBag.FilterModule = module;
            //ViewBag.FilterAction = action;
            ViewBag.FilterIsSuccess = isSuccess;
            ViewBag.FilterDateFrom = dateFrom;
            ViewBag.FilterDateTo = dateTo;

            return View();
        }
    }
}