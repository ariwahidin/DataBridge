using DataBridge.Models.Entities;
using DataBridge.Models.ViewModels.EmailConfig;
using DataBridge.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataBridge.Services
{
    public class EmailConfigService
    {
        private readonly IMirrorJobRepository _jobRepo;
        private readonly Data.AppDbContext _ctx;

        public EmailConfigService(IMirrorJobRepository jobRepo, Data.AppDbContext ctx)
        {
            _jobRepo = jobRepo;
            _ctx = ctx;
        }

        public async Task<EmailConfigListViewModel> GetListAsync()
        {
            var jobs = await _jobRepo.GetAllWithSourceAsync();
            var configs = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .ToListAsync(_ctx.EmailConfigs.Include(e => e.MirrorJob));

            return new EmailConfigListViewModel
            {
                Configs = configs.Select(c => new EmailConfigRowViewModel
                {
                    Id = c.Id,
                    MirrorJobId = c.MirrorJobId,
                    JobName = c.MirrorJob?.Name ?? "-",
                    Recipients = c.Recipients,
                    NotifyOnFail = c.NotifyOnFail,
                    NotifyOnSuccess = c.NotifyOnSuccess,
                    CreatedAt = c.CreatedAt,
                }).ToList()
            };
        }

        public async Task<EmailConfigFormViewModel> GetEmptyFormAsync()
        {
            var jobs = await _jobRepo.GetAllWithSourceAsync();
            return new EmailConfigFormViewModel
            {
                AvailableJobs = jobs.Select(j => new JobOption { Id = j.Id, Name = j.Name }).ToList()
            };
        }

        public async Task<EmailConfigFormViewModel?> GetForEditAsync(int id)
        {
            var c = await _ctx.EmailConfigs.Include(e => e.MirrorJob).FirstOrDefaultAsync(e => e.Id == id);
            if (c == null) return null;
            var jobs = await _jobRepo.GetAllWithSourceAsync();
            return new EmailConfigFormViewModel
            {
                Id = c.Id,
                MirrorJobId = c.MirrorJobId,
                JobName = c.MirrorJob?.Name ?? "",
                Recipients = c.Recipients,
                NotifyOnFail = c.NotifyOnFail,
                NotifyOnSuccess = c.NotifyOnSuccess,
                SmtpHost = c.SmtpHost,
                SmtpPort = c.SmtpPort,
                SmtpUser = c.SmtpUser,
                SmtpPassword = c.SmtpPassword,
                SenderEmail = c.SenderEmail,
                SenderName = c.SenderName,
                AvailableJobs = jobs.Select(j => new JobOption { Id = j.Id, Name = j.Name }).ToList(),
            };
        }

        public async Task<(bool Success, string? Error)> CreateAsync(EmailConfigFormViewModel vm)
        {
            var existing = await _ctx.EmailConfigs.FirstOrDefaultAsync(e => e.MirrorJobId == vm.MirrorJobId);
            if (existing != null) return (false, "This job already has a notification config. Edit it instead.");

            await _ctx.EmailConfigs.AddAsync(new EmailConfig
            {
                MirrorJobId = vm.MirrorJobId,
                Recipients = vm.Recipients.Trim(),
                NotifyOnFail = vm.NotifyOnFail,
                NotifyOnSuccess = vm.NotifyOnSuccess,
                SmtpHost = vm.SmtpHost.Trim(),
                SmtpPort = vm.SmtpPort,
                SmtpUser = vm.SmtpUser.Trim(),
                SmtpPassword = vm.SmtpPassword,
                SenderEmail = vm.SenderEmail.Trim(),
                SenderName = vm.SenderName.Trim(),
                CreatedAt = DateTime.Now,
            });
            await _ctx.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(EmailConfigFormViewModel vm)
        {
            var c = await _ctx.EmailConfigs.FindAsync(vm.Id);
            if (c == null) return (false, "Config not found.");

            c.Recipients = vm.Recipients.Trim();
            c.NotifyOnFail = vm.NotifyOnFail;
            c.NotifyOnSuccess = vm.NotifyOnSuccess;
            c.SmtpHost = vm.SmtpHost.Trim();
            c.SmtpPort = vm.SmtpPort;
            c.SmtpUser = vm.SmtpUser.Trim();
            c.SmtpPassword = vm.SmtpPassword;
            c.SenderEmail = vm.SenderEmail.Trim();
            c.SenderName = vm.SenderName.Trim();
            c.UpdatedAt = DateTime.Now;

            await _ctx.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> DeleteAsync(int id)
        {
            var c = await _ctx.EmailConfigs.FindAsync(id);
            if (c == null) return (false, "Config not found.");
            _ctx.EmailConfigs.Remove(c);
            await _ctx.SaveChangesAsync();
            return (true, null);
        }
    }
}