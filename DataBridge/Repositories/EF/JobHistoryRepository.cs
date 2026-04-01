using DataBridge.Data;
using DataBridge.Models.Entities;
using DataBridge.Models.Enums;
using DataBridge.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataBridge.Repositories.EF
{
    public class JobHistoryRepository : IJobHistoryRepository
    {
        private readonly AppDbContext _ctx;
        public JobHistoryRepository(AppDbContext ctx) => _ctx = ctx;

        public async Task<List<JobHistory>> GetRecentAsync(int take = 50)
            => await _ctx.JobHistories
                .Include(h => h.MirrorJob)
                .OrderByDescending(h => h.StartedAt)
                .Take(take)
                .ToListAsync();

        public async Task<List<JobHistory>> GetByJobIdAsync(int jobId, int take = 100)
            => await _ctx.JobHistories
                .Where(h => h.MirrorJobId == jobId)
                .OrderByDescending(h => h.StartedAt)
                .Take(take)
                .ToListAsync();

        public async Task<(int Total, int Success, int Failed)> GetStatsLast7DaysAsync()
        {
            var since = DateTime.Now.AddDays(-7);
            var items = await _ctx.JobHistories
                .Where(h => h.StartedAt >= since)
                .ToListAsync();
            return (items.Count, items.Count(h => h.Status == JobStatus.Success), items.Count(h => h.Status == JobStatus.Failed));
        }

        public async Task<int> AddAsync(JobHistory history)
        {
            await _ctx.JobHistories.AddAsync(history);
            await _ctx.SaveChangesAsync();
            return history.Id;
        }

        public async Task UpdateAsync(JobHistory history)
        {
            _ctx.JobHistories.Update(history);
            await _ctx.SaveChangesAsync();
        }

        public async Task<JobHistory?> GetByIdAsync(int id)
            => await _ctx.JobHistories.Include(h => h.MirrorJob).FirstOrDefaultAsync(h => h.Id == id);
    }
}