namespace Spikylin.Service.Worker
{
    public sealed class S3ThumbnailSynchronizationWorker(
    S3ThumbnailSynchronizationService synchronizationService,
    ILogger<S3ThumbnailSynchronizationWorker> logger) : BackgroundService
    {
        private readonly TimeSpan interval = TimeSpan.FromSeconds(
            Math.Max(30, 300));

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(interval);
            logger.LogInformation("S3 thumbnail synchronization worker started. Interval: {Interval}", interval);

            do
            {
                try
                {
                    await synchronizationService.SynchronizeAsync(stoppingToken).ConfigureAwait(false);
                    logger.LogInformation("S3 thumbnail synchronization completed.");
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "S3 thumbnail synchronization failed.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
        }
    }
}
