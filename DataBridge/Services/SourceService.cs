using DataBridge.Models.Entities;
using DataBridge.Models.ViewModels.Sources;
using DataBridge.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace DataBridge.Services
{
    public class SourceService
    {
        private readonly ISourceRepository _repo;

        public SourceService(ISourceRepository repo) => _repo = repo;

        public async Task<SourceListViewModel> GetListAsync(string? search)
        {
            var all = await _repo.GetAllAsync();
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                all = all.Where(s => s.Name.ToLower().Contains(search) ||
                    (s.Description ?? "").ToLower().Contains(search)).ToList();
            }
            return new SourceListViewModel
            {
                Sources = all.Select(s => new SourceRowViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    IsActive = s.IsActive,
                    JobCount = s.MirrorJobs.Count,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                }).ToList(),
                Search = search,
            };
        }

        public async Task<SourceFormViewModel?> GetForEditAsync(int id)
        {
            var s = await _repo.GetByIdAsync(id);
            if (s == null) return null;
            return new SourceFormViewModel
            {
                Id = s.Id,
                Name = s.Name,
                ConnectionString = s.ConnectionString,
                Description = s.Description,
                IsActive = s.IsActive,
            };
        }

        public async Task<(bool Success, string? Error)> CreateAsync(SourceFormViewModel vm)
        {
            if (await _repo.ExistsAsync(vm.Name))
                return (false, $"Source name '{vm.Name}' already exists.");

            await _repo.AddAsync(new Source
            {
                Name = vm.Name.Trim(),
                ConnectionString = vm.ConnectionString.Trim(),
                Description = vm.Description?.Trim(),
                IsActive = vm.IsActive,
                CreatedAt = DateTime.Now,
            });
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(SourceFormViewModel vm)
        {
            var s = await _repo.GetByIdAsync(vm.Id);
            if (s == null) return (false, "Source not found.");

            if (await _repo.ExistsAsync(vm.Name, vm.Id))
                return (false, $"Source name '{vm.Name}' already exists.");

            s.Name = vm.Name.Trim();
            s.ConnectionString = vm.ConnectionString.Trim();
            s.Description = vm.Description?.Trim();
            s.IsActive = vm.IsActive;

            await _repo.UpdateAsync(s);
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> DeleteAsync(int id)
        {
            var s = await _repo.GetByIdAsync(id);
            if (s == null) return (false, "Source not found.");
            await _repo.DeleteAsync(id);
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> ToggleActiveAsync(int id)
        {
            var s = await _repo.GetByIdAsync(id);
            if (s == null) return (false, "Source not found.");
            s.IsActive = !s.IsActive;
            await _repo.UpdateAsync(s);
            return (true, null);
        }

        public (bool Success, string Message) TestConnection(string connectionString)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                conn.Open();
                return (true, "Connection successful.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}