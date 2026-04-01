using DataBridge.Data;
using DataBridge.Models.Entities;
using DataBridge.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataBridge.Repositories.EF
{
    public class MirrorJobRepository : IMirrorJobRepository
    {
        private readonly AppDbContext _ctx;
        public MirrorJobRepository(AppDbContext ctx) => _ctx = ctx;

        public async Task<List<MirrorJob>> GetAllWithSourceAsync()
            => await _ctx.MirrorJobs
                .Include(j => j.Source)
                .Include(j => j.Schedule)
                .OrderBy(j => j.Name)
                .ToListAsync();

        public async Task<MirrorJob?> GetByIdWithDetailsAsync(int id)
            => await _ctx.MirrorJobs
                .Include(j => j.Source)
                .Include(j => j.Schedule)
                .Include(j => j.EmailConfig)
                .FirstOrDefaultAsync(j => j.Id == id);

        public async Task AddAsync(MirrorJob job)
        {
            await _ctx.MirrorJobs.AddAsync(job);
            await _ctx.SaveChangesAsync();
        }

        public async Task UpdateAsync(MirrorJob job)
        {
            job.UpdatedAt = DateTime.Now;
            _ctx.MirrorJobs.Update(job);
            await _ctx.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var j = await _ctx.MirrorJobs.FindAsync(id);
            if (j != null) { _ctx.MirrorJobs.Remove(j); await _ctx.SaveChangesAsync(); }
        }
    }
}