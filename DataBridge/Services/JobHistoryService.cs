using DataBridge.Models.ViewModels.JobHistory;
using DataBridge.Repositories.Interfaces;
using ClosedXML.Excel;

namespace DataBridge.Services
{
    public class JobHistoryService
    {
        private readonly IJobHistoryRepository _repo;
        public JobHistoryService(IJobHistoryRepository repo) => _repo = repo;

        public async Task<JobHistoryListViewModel> GetListAsync(int? jobId, string? status)
        {
            var all = jobId.HasValue
                ? await _repo.GetByJobIdAsync(jobId.Value, 200)
                : await _repo.GetRecentAsync(200);

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Models.Enums.JobStatus>(status, out var s))
                all = all.Where(h => h.Status == s).ToList();

            return new JobHistoryListViewModel
            {
                Histories = all.Select(h => new JobHistoryRowViewModel
                {
                    Id = h.Id,
                    MirrorJobId = h.MirrorJobId,
                    JobName = h.MirrorJob?.Name ?? "-",
                    Status = h.Status,
                    StartedAt = h.StartedAt,
                    FinishedAt = h.FinishedAt,
                    RowsAffected = h.RowsAffected,
                    ErrorMessage = h.ErrorMessage,
                    TriggeredBy = h.TriggeredBy,
                }).ToList(),
                FilterJobId = jobId,
                FilterStatus = status,
                TotalCount = all.Count,
            };
        }

        public async Task<byte[]> ExportExcelAsync(int? jobId, string? status)
        {
            var vm = await GetListAsync(jobId, status);
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Job History");

            var headers = new[] { "ID", "Job Name", "Status", "Started At", "Finished At", "Duration (s)", "Rows Affected", "Triggered By", "Error" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2563eb");
                ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            }

            int row = 2;
            foreach (var h in vm.Histories)
            {
                ws.Cell(row, 1).Value = h.Id;
                ws.Cell(row, 2).Value = h.JobName;
                ws.Cell(row, 3).Value = h.Status.ToString();
                ws.Cell(row, 4).Value = h.StartedAt.ToString("yyyy-MM-dd HH:mm:ss");
                ws.Cell(row, 5).Value = h.FinishedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                //ws.Cell(row, 6).Value = h.DurationSeconds.HasValue ? Math.Round(h.DurationSeconds.Value, 2) : (object)"";
                ws.Cell(row, 6).Value = h.DurationSeconds.HasValue
                ? Math.Round(h.DurationSeconds.Value, 2).ToString()
                : "";
                ws.Cell(row, 7).Value = h.RowsAffected?.ToString() ?? "";
                ws.Cell(row, 8).Value = h.TriggeredBy;
                ws.Cell(row, 9).Value = h.ErrorMessage ?? "";
                row++;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }
    }
}