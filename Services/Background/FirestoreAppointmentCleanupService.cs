using MeetAmalfiCoast.Services.Configuration;

namespace MeetAmalfiCoast.Services.BackgroundServices;

public class FirestoreAppointmentCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FirestoreAppointmentCleanupService> _logger;

    public FirestoreAppointmentCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<FirestoreAppointmentCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            DateTime now = DateTime.Now;
            DateTime nextExecution = GetNextExecution(now);

            TimeSpan delay = nextExecution - now;

            _logger.LogInformation(
                "Prossima pulizia degli appuntamenti Firestore prevista per {NextExecution}",
                nextExecution
            );

            await Task.Delay(delay, stoppingToken);

            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();

                FirestorePlanningService planningService =
                    scope.ServiceProvider
                        .GetRequiredService<FirestorePlanningService>();

                int deletedCount =
                    await planningService.DeleteOldAppointmentsAsync();

                _logger.LogInformation(
                    "Pulizia Firestore completata. Eliminati {DeletedCount} appuntamenti.",
                    deletedCount
                );
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Errore durante la pulizia degli appuntamenti Firestore"
                );
            }
        }
    }

    private static DateTime GetNextExecution(DateTime now)
    {
        DateTime firstDayOfNextMonth =
            new DateTime(now.Year, now.Month, 1)
                .AddMonths(1);

        return firstDayOfNextMonth;
    }
}