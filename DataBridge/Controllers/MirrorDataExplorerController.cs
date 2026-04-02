using DataBridge.Filters;
using DataBridge.Models.Enums;
using DataBridge.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace DataBridge.Controllers
{
    [RequireRole(UserRole.Admin)]
    public class MirrorDataExplorerController : Controller
    {
        private readonly MirrorDataExplorerService _svc;

        public MirrorDataExplorerController(MirrorDataExplorerService svc)
        {
            _svc = svc;
        }

        private void SetBreadcrumb(params (string Label, string Url)[] extra)
        {
            var list = new List<(string, string)> { ("Mirror DB", "#") };
            list.AddRange(extra);
            ViewData["Breadcrumbs"] = list;
        }

        // ── 1. Table picker ───────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Data Explorer";
            SetBreadcrumb(("Data Explorer", "/MirrorDataExplorer"));
            var tables = await _svc.GetAllTablesAsync();
            return View(tables);
        }

        // ── 2. Browse a table ─────────────────────────────────────────────────
        public async Task<IActionResult> Browse(string tableName)
        {
            ViewData["Title"] = $"Browse — {tableName}";
            SetBreadcrumb(("Data Explorer", "/MirrorDataExplorer"), (tableName, "#"));

            var columns = await _svc.GetColumnsAsync(tableName);
            ViewBag.TableName = tableName;
            ViewBag.Columns = columns;
            return View();
        }

        // ── 3. Data endpoint (AJAX paging/search/filter) ──────────────────────
        [HttpGet]
        public async Task<IActionResult> GetData(
            string tableName,
            string? search,
            string? filterColumn,
            string? filterValue,
            string? sortColumn,
            string sortDir = "asc",
            int page = 1,
            int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return Json(new { success = false, error = "tableName required" });

            var result = await _svc.GetPagedDataAsync(
                tableName, search, filterColumn, filterValue,
                sortColumn, sortDir, page, pageSize);

            // Preserve original column name casing from SQL — do NOT use camelCase serializer
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null   // keep PascalCase on PagedDataResult props
            });
            return Content(json, "application/json");
        }
    }
}