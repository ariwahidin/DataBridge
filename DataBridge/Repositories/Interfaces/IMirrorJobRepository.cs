using DataBridge.Models.Entities;

namespace DataBridge.Repositories.Interfaces
{
    public interface IMirrorJobRepository
    {
        Task<List<MirrorJob>> GetAllWithSourceAsync();
        Task<MirrorJob?> GetByIdWithDetailsAsync(int id);
        Task AddAsync(MirrorJob job);
        Task UpdateAsync(MirrorJob job);
        Task DeleteAsync(int id);
    }
}