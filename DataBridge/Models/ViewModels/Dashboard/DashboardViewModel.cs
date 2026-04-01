using DataBridge.Models.ViewModels.JobHistory;

namespace DataBridge.Models.ViewModels.Dashboard
{
    public class DashboardViewModel
    {
        public int TotalJobs { get; set; }
        public int ActiveJobs { get; set; }
        public int ActiveSchedules { get; set; }
        public int SuccessLast7Days { get; set; }
        public int FailedLast7Days { get; set; }
        public int TotalSources { get; set; }
        public List<JobHistoryRowViewModel> RecentHistory { get; set; } = [];
    }
}