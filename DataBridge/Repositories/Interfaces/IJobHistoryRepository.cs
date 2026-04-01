using DataBridge.Models.Entities;
using DataBridge.Models.Enums;

namespace DataBridge.Repositories.Interfaces
{
    public interface IJobHistoryRepository
    {
        Task<List<JobHistory>> GetRecentAsync(int take = 50);
        Task<List<JobHistory>> GetByJobIdAsync(int jobId, int take = 100);
        Task<(int Total, int Success, int Failed)> GetStatsLast7DaysAsync();
        Task<int> AddAsync(JobHistory history);
        Task UpdateAsync(JobHistory history);
        Task<JobHistory?> GetByIdAsync(int id);
    }
}