using DataBridge.Services;
using Quartz;

namespace DataBridge.Jobs
{
    /// <summary>
    /// Quartz IJob implementation — called by the scheduler for each mirror job.
    /// Job identity key  = mirrorJobId (stored in JobDataMap).
    /// </summary>
    [DisallowConcurrentExecution]
    public class MirrorSyncJob : IJob
    {
        private readonly MirrorExecutionService _executor;
        private readonly ILogger<MirrorSyncJob> _logger;

        public MirrorSyncJob(MirrorExecutionService executor, ILogger<MirrorSyncJob> logger)
        {
            _executor = executor;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var jobId = context.JobDetail.JobDataMap.GetInt("mirrorJobId");
            _logger.LogInformation("[DataBridge Quartz] Firing job {JobId}", jobId);
            await _executor.ExecuteAsync(jobId, "Scheduler");
        }
    }
}