using Quản_lý_quán_cafe.Services;

namespace Quản_lý_quán_cafe.Services;

/// <summary>
/// Background service that periodically checks for overdue reservations and auto-cancels them.
/// Runs every 5 minutes to ensure timely cancellation of reservations that are 30 minutes late.
/// </summary>
public class ReservationAutoCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReservationAutoCleanupService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public ReservationAutoCleanupService(IServiceProvider serviceProvider, ILogger<ReservationAutoCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reservation Auto-Cleanup Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExecuteCleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Reservation Auto-Cleanup Service");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Reservation Auto-Cleanup Service stopped");
    }

    private async Task ExecuteCleanupAsync(CancellationToken cancellationToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var reservationStatusService = scope.ServiceProvider.GetRequiredService<ReservationStatusService>();
            var cancelledCount = await reservationStatusService.AutoCancelOverdueReservationsAsync(cancellationToken);

            if (cancelledCount > 0)
            {
                _logger.LogInformation("Reservation cleanup completed: {Count} reservations auto-cancelled", cancelledCount);
            }
        }
    }
}
