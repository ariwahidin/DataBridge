using DataBridge.Data;
using DataBridge.Models.Entities;
using DataBridge.Models.ViewModels.Schedules;
using DataBridge.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataBridge.Services
{
    public class ScheduleService
    {
        private readonly IMirrorJobRepository _jobRepo;
        private readonly AppDbContext _ctx;
        private readonly QuartzSchedulerManager _quartz;

        public ScheduleService(IMirrorJobRepository jobRepo, AppDbContext ctx, QuartzSchedulerManager quartz)
        {
            _jobRepo = jobRepo;
            _ctx = ctx;
            _quartz = quartz;
        }

        public async Task<ScheduleListViewModel> GetListAsync()
        {
            var jobs = await _jobRepo.GetAllWithSourceAsync();
            return new ScheduleListViewModel
            {
                Schedules = jobs.Where(j => j.Schedule != null).Select(j => new ScheduleRowViewModel
                {
                    Id = j.Schedule!.Id,
                    MirrorJobId = j.Id,
                    JobName = j.Name,
                    SourceName = j.Source.Name,
                    CronExpression = j.Schedule.CronExpression,
                    CronLabel = QuartzSchedulerManager.ToLabel(j.Schedule.CronExpression),
                    IsEnabled = j.Schedule.IsEnabled,
                    LastRunAt = j.Schedule.LastRunAt,
                    NextRunAt = j.Schedule.NextRunAt,
                    CreatedAt = j.Schedule.CreatedAt,
                }).ToList()
            };
        }

        public async Task<ScheduleFormViewModel?> GetForJobAsync(int mirrorJobId)
        {
            var job = await _jobRepo.GetByIdWithDetailsAsync(mirrorJobId);
            if (job == null) return null;
            return new ScheduleFormViewModel
            {
                Id = job.Schedule?.Id ?? 0,
                MirrorJobId = mirrorJobId,
                JobName = job.Name,
                CronExpression = job.Schedule?.CronExpression ?? "every:1:hour",
                IsEnabled = job.Schedule?.IsEnabled ?? true,
            };
        }

        public async Task<(bool Success, string? Error)> SaveAsync(ScheduleFormViewModel vm)
        {
            var job = await _jobRepo.GetByIdWithDetailsAsync(vm.MirrorJobId);
            if (job == null) return (false, "Mirror job not found.");

            if (job.Schedule == null)
            {
                var schedule = new JobSchedule
                {
                    MirrorJobId = vm.MirrorJobId,
                    CronExpression = vm.CronExpression.Trim(),
                    IsEnabled = vm.IsEnabled,
                    CreatedAt = DateTime.Now,
                };
                _ctx.JobSchedules.Add(schedule);
            }
            else
            {
                job.Schedule.CronExpression = vm.CronExpression.Trim();
                job.Schedule.IsEnabled = vm.IsEnabled;
                job.Schedule.UpdatedAt = DateTime.Now;
            }

            await _ctx.SaveChangesAsync();

            // Sync Quartz
            if (vm.IsEnabled && job.IsActive)
                await _quartz.ScheduleJobAsync(vm.MirrorJobId, vm.CronExpression.Trim());
            else
                await _quartz.UnscheduleJobAsync(vm.MirrorJobId);

            return (true, null);
        }

        public async Task<(bool Success, string? Error)> ToggleAsync(int scheduleId)
        {
            var s = await _ctx.JobSchedules.Include(x => x.MirrorJob).FirstOrDefaultAsync(x => x.Id == scheduleId);
            if (s == null) return (false, "Schedule not found.");
            s.IsEnabled = !s.IsEnabled;
            s.UpdatedAt = DateTime.Now;
            await _ctx.SaveChangesAsync();

            if (s.IsEnabled && s.MirrorJob.IsActive)
                await _quartz.ScheduleJobAsync(s.MirrorJobId, s.CronExpression);
            else
                await _quartz.UnscheduleJobAsync(s.MirrorJobId);

            return (true, null);
        }

        public async Task<(bool Success, string? Error)> DeleteAsync(int scheduleId)
        {
            var s = await _ctx.JobSchedules.FindAsync(scheduleId);
            if (s == null) return (false, "Schedule not found.");
            await _quartz.UnscheduleJobAsync(s.MirrorJobId);
            _ctx.JobSchedules.Remove(s);
            await _ctx.SaveChangesAsync();
            return (true, null);
        }
    }
}