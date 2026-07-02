using MeetAmalfiCoast.Models;

public class AppointmentReminderService : BackgroundService
{
    private readonly FirestorePlanningService _firestorePlanningService;
    private readonly EmailService _emailService;
    private readonly ILogger<AppointmentReminderService> _logger;

    public AppointmentReminderService(
        FirestorePlanningService firestorePlanningService,
        EmailService emailService,
        ILogger<AppointmentReminderService> logger)
    {
        _firestorePlanningService = firestorePlanningService;
        _emailService = emailService;
        _logger = logger;
    }

    // Esegue il servizio in background per inviare promemoria appuntamenti via email.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendReminderEmailsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'invio dei promemoria appuntamento.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    // Invia i promemoria appuntamenti via email.
    private async Task SendReminderEmailsAsync()
    {
        List<PlanningAppointment> appointments =
            await _firestorePlanningService.GetAppointmentsForReminderAsync();

        foreach (PlanningAppointment appointment in appointments)
        {
            if (string.IsNullOrWhiteSpace(appointment.CustomerEmail))
            {
                continue;
            }

            await _emailService.SendAppointmentReminderAsync(appointment);

            await _firestorePlanningService.MarkReminderEmailAsSentAsync(appointment.Id);

            _logger.LogInformation(
                "Promemoria appuntamento inviato a {Email} per appuntamento {AppointmentId}.",
                appointment.CustomerEmail,
                appointment.Id
            );
        }
    }
}