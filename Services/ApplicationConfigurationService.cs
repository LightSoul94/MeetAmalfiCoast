using Microsoft.Extensions.Options;

namespace MeetAmalfiCoast.Services.Configuration;

public class ApplicationConfigurationService
{
    private readonly ApplicationSettings _settings;

    public ApplicationConfigurationService(
        IOptions<ApplicationSettings> applicationOptions)
    {
        _settings = applicationOptions.Value;
    }

    public bool IsDebugMode => _settings.Debug;

    public int OrarioMinimoPrenotabile =>
    _settings.OrarioMinimoPrenotabile;

    public string BaseUrl =>
        _settings.Debug
            ? "http://localhost:5087"
            : "https://meetamalficoasts.com";

    public int AppointmentReminderLeadDays =>
        _settings.Debug
            ? 0
            : 1;

    public TimeSpan AppointmentReminderInitialDelay =>
        _settings.Debug
            ? TimeSpan.FromSeconds(10)
            : TimeSpan.FromMinutes(1);

    public TimeSpan AppointmentReminderCheckInterval =>
        _settings.Debug
            ? TimeSpan.FromSeconds(10)
            : TimeSpan.FromHours(1);

    public TimeSpan NewsletterReminderCheckInterval =>
        _settings.Debug
            ? TimeSpan.FromSeconds(30)
            : TimeSpan.FromDays(1);

    public int NewsletterReminderIntervalDays =>
        _settings.Debug
            ? 0
            : 30;

    public int FirestoreAppointmentRetentionDays =>
    _settings.Debug
        ? 1
        : 365;
}