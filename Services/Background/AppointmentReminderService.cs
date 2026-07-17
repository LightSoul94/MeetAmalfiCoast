using MeetAmalfiCoast.Models;
using MeetAmalfiCoast.Services.Configuration;

public class AppointmentReminderService : BackgroundService
{
    private readonly ApplicationConfigurationService _configuration;
    private readonly FirestorePlanningService _firestorePlanningService;
    private readonly EmailService _emailService;
    private readonly ILogger<AppointmentReminderService> _logger;

    public AppointmentReminderService(
        ApplicationConfigurationService configuration,
        FirestorePlanningService firestorePlanningService,
        EmailService emailService,
        ILogger<AppointmentReminderService> logger)
    {
        _configuration = configuration;
        _firestorePlanningService = firestorePlanningService;
        _emailService = emailService;
        _logger = logger;
    }

    // Esegue il servizio in background per inviare promemoria appuntamenti via email.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(_configuration.AppointmentReminderInitialDelay, stoppingToken);

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

            await Task.Delay(_configuration.AppointmentReminderCheckInterval, stoppingToken);
        }
    }

    // Invia i promemoria appuntamenti via email.
    private async Task SendReminderEmailsAsync()
    {
        List<PlanningAppointmentModel> appointments =
            await _firestorePlanningService.GetAppointmentsForReminderAsync();

        foreach (PlanningAppointmentModel appointment in appointments)
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