namespace DataBridge.Services
{
    /// <summary>
    /// Loads all DB schedules into Quartz at app startup.
    /// </summary>
    public class SchedulerHostedService : IHostedService
    {
        private readonly QuartzSchedulerManager _mgr;
        private readonly ILogger<SchedulerHostedService> _logger;

        public SchedulerHostedService(QuartzSchedulerManager mgr, ILogger<SchedulerHostedService> logger)
        {
            _mgr = mgr;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[DataBridge] Loading schedules from database...");
            await _mgr.SyncAllSchedulesAsync();
            _logger.LogInformation("[DataBridge] Schedules loaded.");
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}