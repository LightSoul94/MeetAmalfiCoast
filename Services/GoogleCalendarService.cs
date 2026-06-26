using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Options;

public class GoogleCalendarService
{
    private readonly GoogleCalendarSettings _settings;
    private readonly FirestorePlanningService _firestorePlanningService;
    private readonly ILogger<GoogleCalendarService> _logger;

    private readonly string _tokenFilePath;
    private readonly string _syncTokenFilePath;

    public GoogleCalendarService(
        IOptions<GoogleCalendarSettings> settings,
        FirestorePlanningService firestorePlanningService,
        ILogger<GoogleCalendarService> logger)
    {
        _settings = settings.Value;
        _firestorePlanningService = firestorePlanningService;
        _logger = logger;

        _tokenFilePath = Path.Combine(AppContext.BaseDirectory, "google-calendar-token.json");
        _syncTokenFilePath = Path.Combine(AppContext.BaseDirectory, "google-calendar-sync-token.txt");
    }

    public string GetAuthorizationUrl()
    {
        return "https://accounts.google.com/o/oauth2/v2/auth" +
               "?client_id=" + Uri.EscapeDataString(_settings.ClientId) +
               "&redirect_uri=" + Uri.EscapeDataString(_settings.RedirectUri) +
               "&response_type=code" +
               "&scope=" + Uri.EscapeDataString(CalendarService.Scope.Calendar) +
               "&access_type=offline" +
               "&prompt=consent";
    }

    public async Task SaveTokenAsync(string code)
    {
        var flow = CreateAuthorizationFlow();

        TokenResponse token = await flow.ExchangeCodeForTokenAsync(
            userId: "default-user",
            code: code,
            redirectUri: _settings.RedirectUri,
            taskCancellationToken: CancellationToken.None
        );

        string json = System.Text.Json.JsonSerializer.Serialize(token);

        await File.WriteAllTextAsync(_tokenFilePath, json);

        if (File.Exists(_syncTokenFilePath))
        {
            File.Delete(_syncTokenFilePath);
        }
    }

    public async Task<string> CreateEventAsync(PlanningAppointment appointment)
    {
        CalendarService service = await CreateCalendarServiceAsync();

        DateTime startDateTime = DateTime.Parse($"{appointment.IsoDate} {appointment.Start}");
        DateTime endDateTime = DateTime.Parse($"{appointment.IsoDate} {appointment.End}");

        Event googleEvent = new Event
        {
            Summary = appointment.Title,
            Description =
                $"Cliente: {appointment.Customer}\n" +
                $"Email: {appointment.CustomerEmail}",

            Start = new EventDateTime
            {
                DateTimeDateTimeOffset = new DateTimeOffset(startDateTime),
                TimeZone = "Europe/Rome"
            },

            End = new EventDateTime
            {
                DateTimeDateTimeOffset = new DateTimeOffset(endDateTime),
                TimeZone = "Europe/Rome"
            }
        };

        Event createdEvent = await service.Events.Insert(googleEvent, "primary").ExecuteAsync();

        return createdEvent.Id;
    }

    public async Task SyncGoogleToFirestoreAsync()
    {
        var changedEvents = await GetChangedEventsAsync();

        if (changedEvents.Count == 0)
        {
            return;
        }

        foreach (var googleEvent in changedEvents)
        {
            if (string.IsNullOrWhiteSpace(googleEvent.Id))
            {
                continue;
            }

            if (googleEvent.Status == "cancelled")
            {
                await _firestorePlanningService.DeleteAppointmentByGoogleEventIdAsync(
                    googleEvent.Id
                );

                _logger.LogInformation(
                    "Evento eliminato da Firestore. GoogleEventId: {GoogleEventId}",
                    googleEvent.Id
                );

                continue;
            }

            DateTime? startDateTime = googleEvent.Start?.DateTimeDateTimeOffset?.DateTime;
            DateTime? endDateTime = googleEvent.End?.DateTimeDateTimeOffset?.DateTime;

            if (startDateTime == null || endDateTime == null)
            {
                continue;
            }

            await _firestorePlanningService.UpsertAppointmentFromGoogleAsync(
                googleEventId: googleEvent.Id,
                title: googleEvent.Summary ?? "Appuntamento",
                startDateTime: startDateTime.Value,
                endDateTime: endDateTime.Value
            );

            _logger.LogInformation(
                "Evento aggiornato da Google Calendar verso Firestore. GoogleEventId: {GoogleEventId}",
                googleEvent.Id
            );
        }
    }

    public async Task<List<Event>> GetChangedEventsAsync()
    {
        CalendarService service = await CreateCalendarServiceAsync();

        if (!File.Exists(_syncTokenFilePath))
        {
            await EnsureSyncTokenAsync(service);
            return new List<Event>();
        }

        string syncToken = await File.ReadAllTextAsync(_syncTokenFilePath);

        EventsResource.ListRequest request = service.Events.List("primary");
        request.SyncToken = syncToken;
        request.ShowDeleted = true;

        try
        {
            Events events = await request.ExecuteAsync();

            if (!string.IsNullOrWhiteSpace(events.NextSyncToken))
            {
                await File.WriteAllTextAsync(_syncTokenFilePath, events.NextSyncToken);
            }

            return events.Items?.ToList() ?? new List<Event>();
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.Gone)
        {
            if (File.Exists(_syncTokenFilePath))
            {
                File.Delete(_syncTokenFilePath);
            }

            await EnsureSyncTokenAsync(service);

            return new List<Event>();
        }
    }

    private async Task EnsureSyncTokenAsync(CalendarService service)
    {
        if (File.Exists(_syncTokenFilePath))
        {
            return;
        }

        EventsResource.ListRequest request = service.Events.List("primary");
        request.ShowDeleted = true;
        request.SingleEvents = true;
        request.MaxResults = 2500;

        Events events = await request.ExecuteAsync();

        if (!string.IsNullOrWhiteSpace(events.NextSyncToken))
        {
            await File.WriteAllTextAsync(_syncTokenFilePath, events.NextSyncToken);
        }
    }

    private GoogleAuthorizationCodeFlow CreateAuthorizationFlow()
    {
        return new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = _settings.ClientId,
                ClientSecret = _settings.ClientSecret
            },
            Scopes = new[]
            {
                CalendarService.Scope.Calendar
            }
        });
    }

    private async Task<CalendarService> CreateCalendarServiceAsync()
    {
        if (!File.Exists(_tokenFilePath))
        {
            throw new Exception("Google Calendar non collegato. Effettua prima la connessione.");
        }

        string json = await File.ReadAllTextAsync(_tokenFilePath);

        TokenResponse token = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(json)
            ?? throw new Exception("Token Google Calendar non valido.");

        var flow = CreateAuthorizationFlow();

        var credential = new UserCredential(
            flow,
            "default-user",
            token
        );

        if (credential.Token.IsStale)
        {
            bool refreshed = await credential.RefreshTokenAsync(CancellationToken.None);

            if (!refreshed)
            {
                throw new Exception("Token Google scaduto. Ricollega Google Calendar.");
            }

            string refreshedJson = System.Text.Json.JsonSerializer.Serialize(credential.Token);

            await File.WriteAllTextAsync(_tokenFilePath, refreshedJson);
        }

        return new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Meet Amalfi Coasts"
        });
    }

    public async Task<List<Event>> GetFutureEventsAsync()
    {
        CalendarService service = await CreateCalendarServiceAsync();

        EventsResource.ListRequest request = service.Events.List("primary");

        request.TimeMinDateTimeOffset = DateTimeOffset.Now;
        request.ShowDeleted = false;
        request.SingleEvents = true;
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
        request.MaxResults = 2500;

        Events events = await request.ExecuteAsync();

        return events.Items?.ToList() ?? new List<Event>();
    }

    public async Task ResetPlanningFromGoogleCalendarAsync()
    {
        CalendarService service = await CreateCalendarServiceAsync();

        EventsResource.ListRequest request = service.Events.List("primary");

        request.TimeMinDateTimeOffset = DateTimeOffset.Now;
        request.ShowDeleted = false;
        request.SingleEvents = true;
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
        request.MaxResults = 2500;

        Events events = await request.ExecuteAsync();

        await _firestorePlanningService.ClearAllAppointmentsAsync();

        foreach (Event googleEvent in events.Items ?? new List<Event>())
        {
            if (string.IsNullOrWhiteSpace(googleEvent.Id))
            {
                continue;
            }

            DateTime? startDateTime = googleEvent.Start?.DateTimeDateTimeOffset?.DateTime;
            DateTime? endDateTime = googleEvent.End?.DateTimeDateTimeOffset?.DateTime;

            if (startDateTime == null || endDateTime == null)
            {
                continue;
            }

            await _firestorePlanningService.UpsertAppointmentFromGoogleAsync(
                googleEventId: googleEvent.Id,
                title: googleEvent.Summary ?? "Appuntamento",
                startDateTime: startDateTime.Value,
                endDateTime: endDateTime.Value
            );
        }

        await ResetSyncTokenAsync(service);
    }

    private async Task ResetSyncTokenAsync(CalendarService service)
    {
        if (File.Exists(_syncTokenFilePath))
        {
            File.Delete(_syncTokenFilePath);
        }

        EventsResource.ListRequest request = service.Events.List("primary");
        request.ShowDeleted = true;
        request.SingleEvents = true;
        request.MaxResults = 2500;

        Events events = await request.ExecuteAsync();

        if (!string.IsNullOrWhiteSpace(events.NextSyncToken))
        {
            await File.WriteAllTextAsync(_syncTokenFilePath, events.NextSyncToken);
        }
    }
}