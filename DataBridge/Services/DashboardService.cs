using DataBridge.Models.ViewModels.Dashboard;
using DataBridge.Models.ViewModels.JobHistory;
using DataBridge.Repositories.Interfaces;

namespace DataBridge.Services
{
    public class DashboardService
    {
        private readonly IMirrorJobRepository _jobRepo;
        private readonly ISourceRepository _sourceRepo;
        private readonly IJobHistoryRepository _historyRepo;

        public DashboardService(IMirrorJobRepository jobRepo, ISourceRepository sourceRepo, IJobHistoryRepository historyRepo)
        {
            _jobRepo = jobRepo;
            _sourceRepo = sourceRepo;
            _historyRepo = historyRepo;
        }

        public async Task<DashboardViewModel> GetAsync()
        {
            var jobs = await _jobRepo.GetAllWithSourceAsync();
            var sources = await _sourceRepo.GetAllAsync();
            var recent = await _historyRepo.GetRecentAsync(10);
            var (_, success, failed) = await _historyRepo.GetStatsLast7DaysAsync();

            return new DashboardViewModel
            {
                TotalJobs = jobs.Count,
                ActiveJobs = jobs.Count(j => j.IsActive),
                ActiveSchedules = jobs.Count(j => j.Schedule?.IsEnabled == true),
                SuccessLast7Days = success,
                FailedLast7Days = failed,
                TotalSources = sources.Count,
                RecentHistory = recent.Select(h => new JobHistoryRowViewModel
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
            };
        }
    }
}