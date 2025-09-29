namespace MatchFynWebAPI.Services.Background
{
    /// <summary>
    /// Background service host for room management
    /// Runs 7/24 automatic room management tasks
    /// </summary>
    public class RoomManagementBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RoomManagementBackgroundService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(1); // Run every minute

        public RoomManagementBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<RoomManagementBackgroundService> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Room Management Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunMaintenanceTasks();
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Room Management Background Service is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Room Management Background Service");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait 5 minutes on error
                }
            }

            _logger.LogInformation("Room Management Background Service stopped");
        }

        private async Task RunMaintenanceTasks()
        {
            var startTime = DateTime.UtcNow;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var roomManagementService = scope.ServiceProvider.GetRequiredService<IRoomManagementService>();

                // Run all maintenance tasks
                await roomManagementService.CleanupExpiredRoomsAsync();
                await roomManagementService.HandleInactiveParticipantsAsync();
                await roomManagementService.CreateWaitingRoomsAsync();
                await roomManagementService.PromoteWaitingRoomsAsync();
                await roomManagementService.CreateMatchingRoomsAsync();

                // Run health monitoring every 5 minutes
                if (DateTime.UtcNow.Minute % 5 == 0)
                {
                    await roomManagementService.MonitorRoomHealthAsync();
                }

                var duration = DateTime.UtcNow - startTime;
                _logger.LogDebug("Room maintenance tasks completed in {Duration}ms", duration.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running room maintenance tasks");
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Room Management Background Service is stopping...");
            await base.StopAsync(stoppingToken);
        }
    }
}
