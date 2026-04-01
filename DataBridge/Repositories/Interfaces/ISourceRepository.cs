using DataBridge.Models.Entities;

namespace DataBridge.Repositories.Interfaces
{
    public interface ISourceRepository
    {
        Task<List<Source>> GetAllAsync();
        Task<Source?> GetByIdAsync(int id);
        Task AddAsync(Source source);
        Task UpdateAsync(Source source);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(string name, int? excludeId = null);
    }
}