using DataBridge.Models.Entities;
using DataBridge.Models.Enums;
using DataBridge.Models.ViewModels.MirrorJobs;
using DataBridge.Repositories.Interfaces;

namespace DataBridge.Services
{
    public class MirrorJobService
    {
        private readonly IMirrorJobRepository _jobRepo;
        private readonly ISourceRepository _sourceRepo;

        public MirrorJobService(IMirrorJobRepository jobRepo, ISourceRepository sourceRepo)
        {
            _jobRepo = jobRepo;
            _sourceRepo = sourceRepo;
        }

        public async Task<MirrorJobListViewModel> GetListAsync(string? search)
        {
            var all = await _jobRepo.GetAllWithSourceAsync();
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                all = all.Where(j => j.Name.ToLower().Contains(search) ||
                    j.Source.Name.ToLower().Contains(search) ||
                    j.DestinationTable.ToLower().Contains(search)).ToList();
            }
            return new MirrorJobListViewModel
            {
                Jobs = all.Select(j => new MirrorJobRowViewModel
                {
                    Id = j.Id,
                    Name = j.Name,
                    SourceName = j.Source.Name,
                    DestinationTable = j.DestinationTable,
                    SyncMode = j.SyncMode,
                    IsActive = j.IsActive,
                    HasSchedule = j.Schedule != null,
                    ScheduleEnabled = j.Schedule?.IsEnabled ?? false,
                    CronExpression = j.Schedule?.CronExpression,
                    LastRunAt = j.Schedule?.LastRunAt,
                    CreatedAt = j.CreatedAt,
                }).ToList(),
                Search = search,
            };
        }

        public async Task<MirrorJobFormViewModel> GetEmptyFormAsync()
        {
            var sources = await _sourceRepo.GetAllAsync();
            return new MirrorJobFormViewModel
            {
                AvailableSources = sources.Where(s => s.IsActive)
                    .Select(s => new SourceOption { Id = s.Id, Name = s.Name }).ToList()
            };
        }

        public async Task<MirrorJobFormViewModel?> GetForEditAsync(int id)
        {
            var job = await _jobRepo.GetByIdWithDetailsAsync(id);
            if (job == null) return null;
            var sources = await _sourceRepo.GetAllAsync();
            return new MirrorJobFormViewModel
            {
                Id = job.Id,
                Name = job.Name,
                SourceId = job.SourceId,
                SourceQuery = job.SourceQuery,
                DestinationTable = job.DestinationTable,
                SyncMode = job.SyncMode,
                WatermarkColumn = job.WatermarkColumn,
                IsActive = job.IsActive,
                AvailableSources = sources.Where(s => s.IsActive)
                    .Select(s => new SourceOption { Id = s.Id, Name = s.Name }).ToList(),
            };
        }

        public async Task<(bool Success, string? Error)> CreateAsync(MirrorJobFormViewModel vm)
        {
            var job = new MirrorJob
            {
                Name = vm.Name.Trim(),
                SourceId = vm.SourceId,
                SourceQuery = vm.SourceQuery.Trim(),
                DestinationTable = vm.DestinationTable.Trim(),
                SyncMode = vm.SyncMode,
                WatermarkColumn = vm.SyncMode == SyncMode.Incremental ? vm.WatermarkColumn?.Trim() : null,
                IsActive = vm.IsActive,
                CreatedAt = DateTime.Now,
            };
            await _jobRepo.AddAsync(job);
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(MirrorJobFormViewModel vm)
        {
            var job = await _jobRepo.GetByIdWithDetailsAsync(vm.Id);
            if (job == null) return (false, "Job not found.");

            job.Name = vm.Name.Trim();
            job.SourceId = vm.SourceId;
            job.SourceQuery = vm.SourceQuery.Trim();
            job.DestinationTable = vm.DestinationTable.Trim();
            job.SyncMode = vm.SyncMode;
            job.WatermarkColumn = vm.SyncMode == SyncMode.Incremental ? vm.WatermarkColumn?.Trim() : null;
            job.IsActive = vm.IsActive;

            await _jobRepo.UpdateAsync(job);
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> DeleteAsync(int id)
        {
            var job = await _jobRepo.GetByIdWithDetailsAsync(id);
            if (job == null) return (false, "Job not found.");
            await _jobRepo.DeleteAsync(id);
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> ToggleActiveAsync(int id)
        {
            var job = await _jobRepo.GetByIdWithDetailsAsync(id);
            if (job == null) return (false, "Job not found.");
            job.IsActive = !job.IsActive;
            await _jobRepo.UpdateAsync(job);
            return (true, null);
        }
    }
}