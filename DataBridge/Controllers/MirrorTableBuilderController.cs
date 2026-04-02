using DataBridge.Filters;
using DataBridge.Models.Enums;
using DataBridge.Models.ViewModels.MirrorTableBuilder;
using DataBridge.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataBridge.Controllers
{
    [RequireRole(UserRole.Admin)]
    public class MirrorTableBuilderController : Controller
    {
        private readonly MirrorTableBuilderService _svc;

        public MirrorTableBuilderController(MirrorTableBuilderService svc)
        {
            _svc = svc;
        }

        private void SetBreadcrumb(params (string Label, string Url)[] extra)
        {
            var list = new List<(string, string)> { ("Mirror DB", "#") };
            list.AddRange(extra);
            ViewData["Breadcrumbs"] = list;
        }

        // ── 1. List tables ────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Table Builder";
            SetBreadcrumb(("Table Builder", "/MirrorTableBuilder"));
            var tables = await _svc.GetAllTablesAsync();
            return View(tables);
        }

        // ── 2. Create table form ──────────────────────────────────────────────
        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "Create Table";
            SetBreadcrumb(("Table Builder", "/MirrorTableBuilder"), ("Create", "#"));
            return View(new CreateTableViewModel());
        }

        // ── 3. Execute CREATE TABLE ───────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTableViewModel vm)
        {
            ViewData["Title"] = "Create Table";
            SetBreadcrumb(("Table Builder", "/MirrorTableBuilder"), ("Create", "#"));

            if (!ModelState.IsValid) return View(vm);

            // Remove empty rows submitted from UI
            vm.Columns = vm.Columns
                .Where(c => !string.IsNullOrWhiteSpace(c.ColumnName))
                .ToList();

            if (!vm.Columns.Any())
            {
                ModelState.AddModelError("", "At least one column is required.");
                return View(vm);
            }

            var (ok, err) = await _svc.CreateTableAsync(vm);
            if (!ok)
            {
                ModelState.AddModelError("", err!);
                return View(vm);
            }

            TempData["Success"] = $"Table '{vm.TableName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ── 4. Drop table ─────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Drop(string tableName)
        {
            var (ok, err) = await _svc.DropTableAsync(tableName);
            if (ok) TempData["Success"] = $"Table '{tableName}' dropped.";
            else TempData["Error"] = err;
            return RedirectToAction(nameof(Index));
        }

        // ── 5. Get columns of existing table (AJAX) ───────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetColumns(string tableName)
        {
            var cols = await _svc.GetColumnsAsync(tableName);
            return Json(cols);
        }

        // ── 6. Edit schema form ───────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(string tableName)
        {
            ViewData["Title"] = $"Edit Schema — {tableName}";
            SetBreadcrumb(("Table Builder", "/MirrorTableBuilder"), ("Edit", "#"));

            var vm = await _svc.GetEditSchemaAsync(tableName);
            if (vm == null)
            {
                TempData["Error"] = $"Table '{tableName}' not found or has no columns.";
                return RedirectToAction(nameof(Index));
            }
            return View(vm);
        }

        // ── 7. Apply schema edits ─────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditSchemaViewModel vm)
        {
            ViewData["Title"] = $"Edit Schema — {vm.FullTableName}";
            SetBreadcrumb(("Table Builder", "/MirrorTableBuilder"), ("Edit", "#"));

            var (ok, err) = await _svc.ApplySchemaEditAsync(vm);
            if (!ok)
            {
                TempData["Error"] = err;
                // Reload existing columns dari DB supaya halaman tetap tampil benar
                var fresh = await _svc.GetEditSchemaAsync(vm.FullTableName);
                if (fresh != null) vm.ExistingColumns = fresh.ExistingColumns;
                return View(vm);
            }

            TempData["Success"] = $"Schema for '{vm.FullTableName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ── 8. Rename table form (modal POST) ─────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Rename(string tableName, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                TempData["Error"] = "New table name cannot be empty.";
                return RedirectToAction(nameof(Index));
            }

            var (ok, err) = await _svc.RenameTableAsync(tableName, newName);
            if (ok) TempData["Success"] = $"Table renamed to '{newName}'.";
            else TempData["Error"] = err;

            return RedirectToAction(nameof(Index));
        }
    }
}