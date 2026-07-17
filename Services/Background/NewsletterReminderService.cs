using MeetAmalfiCoast.Services.Configuration;

public class NewsletterReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NewsletterReminderService> _logger;
    private readonly EmailService _emailService;
    private readonly ApplicationConfigurationService _configuration;


    public NewsletterReminderService(
        IServiceScopeFactory scopeFactory,
        ILogger<NewsletterReminderService> logger,
        EmailService emailService,
        ApplicationConfigurationService configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _emailService = emailService;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        await Task.Delay(_configuration.AppointmentReminderInitialDelay, stoppingToken);

        using PeriodicTimer timer = new(_configuration.NewsletterReminderCheckInterval);

        do
        {
            try
            {
                await ProcessMonthlyRemindersAsync(
                    stoppingToken
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
                    "Errore durante l'invio dei promemoria newsletter."
                );
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessMonthlyRemindersAsync(
        CancellationToken cancellationToken)
    {
        using IServiceScope scope =
            _scopeFactory.CreateScope();

        FirestoreNewsletterService newsletterService =
            scope.ServiceProvider
                .GetRequiredService<FirestoreNewsletterService>();

        NewsletterEmailTemplateService templateService =
            scope.ServiceProvider
                .GetRequiredService<NewsletterEmailTemplateService>();

        EmailService emailService =
            scope.ServiceProvider
                .GetRequiredService<EmailService>();

        List<NewsletterSubscriptionModel> subscribers =
            await newsletterService
                .GetSubscribersForMonthlyReminderAsync();

        if (subscribers.Count == 0)
        {
            return;
        }

        foreach (NewsletterSubscriptionModel subscriber in subscribers)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                string unsubscribeUrl =
                    $"{_configuration.BaseUrl}/Home/UnsubscribeNewsletter?token={subscriber.UnsubscribeToken}";

                string emailBody =
                    await templateService
                        .BuildReminderEmailAsync(unsubscribeUrl);

                await emailService.SendAsync(
                    subscriber.Email,
                    "The Amalfi Coast is waiting for you",
                    emailBody
                );

                await newsletterService
                    .MarkReminderEmailAsSentAsync(subscriber.Id);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Errore durante l'invio del promemoria newsletter a {Email}.",
                    subscriber.Email
                );
            }
        }
    }
}