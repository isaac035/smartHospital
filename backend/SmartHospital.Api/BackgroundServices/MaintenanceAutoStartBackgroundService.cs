using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.BackgroundServices;

public class MaintenanceAutoStartBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MaintenanceAutoStartBackgroundService> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    public MaintenanceAutoStartBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<MaintenanceAutoStartBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Maintenance Auto-Start Background Service started.");

        // Brief delay on startup before first check
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var maintenanceService = scope.ServiceProvider.GetRequiredService<IResourceMaintenanceService>();
                var startedCount = await maintenanceService.ProcessScheduledMaintenanceAutoStartAsync(stoppingToken);

                if (startedCount > 0)
                {
                    _logger.LogInformation("Maintenance Auto-Start cycle: {Count} task(s) transitioned to InProgress.", startedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during Maintenance Auto-Start check.");
            }

            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Maintenance Auto-Start Background Service stopped.");
    }
}
