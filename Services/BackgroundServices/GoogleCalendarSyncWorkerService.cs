using Microsoft.Extensions.Hosting;

public class GoogleCalendarSyncWorkerService : BackgroundService
{
    private readonly GoogleCalendarService _googleCalendarService;
    private readonly ILogger<GoogleCalendarSyncWorkerService> _logger;

    public GoogleCalendarSyncWorkerService  (
        GoogleCalendarService googleCalendarService,
        ILogger<GoogleCalendarSyncWorkerService> logger)
    {
        _googleCalendarService = googleCalendarService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Google Calendar Sync Worker avviato.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _googleCalendarService.SyncGoogleToFirestoreAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la sincronizzazione Google Calendar verso Firestore.");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}