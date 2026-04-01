using DataBridge.Data;
using DataBridge.Models.Entities;
using DataBridge.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataBridge.Repositories.EF
{
    public class SourceRepository : ISourceRepository
    {
        private readonly AppDbContext _ctx;
        public SourceRepository(AppDbContext ctx) => _ctx = ctx;

        public async Task<List<Source>> GetAllAsync()
            => await _ctx.Sources.OrderBy(s => s.Name).ToListAsync();

        public async Task<Source?> GetByIdAsync(int id)
            => await _ctx.Sources.FindAsync(id);

        public async Task AddAsync(Source source)
        {
            await _ctx.Sources.AddAsync(source);
            await _ctx.SaveChangesAsync();
        }

        public async Task UpdateAsync(Source source)
        {
            source.UpdatedAt = DateTime.Now;
            _ctx.Sources.Update(source);
            await _ctx.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var s = await _ctx.Sources.FindAsync(id);
            if (s != null) { _ctx.Sources.Remove(s); await _ctx.SaveChangesAsync(); }
        }

        public async Task<bool> ExistsAsync(string name, int? excludeId = null)
            => await _ctx.Sources.AnyAsync(s => s.Name == name && (excludeId == null || s.Id != excludeId));
    }
}