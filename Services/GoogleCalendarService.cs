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
    private readonly string _tokenFilePath;
    public string WebhookUrl { get; set; } = string.Empty;

    public GoogleCalendarService(IOptions<GoogleCalendarSettings> settings)
    {
        _settings = settings.Value;
        _tokenFilePath = Path.Combine(AppContext.BaseDirectory, "google-calendar-token.json");
    }

    public string GetAuthorizationUrl()
    {
        // Console.WriteLine("=================================");
        // Console.WriteLine("REDIRECT URI: " + _settings.RedirectUri);
        // Console.WriteLine("CLIENT ID: " + _settings.ClientId);
        // Console.WriteLine("=================================");

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

    public Task SyncDeletedOrChangedEventsAsync()
    {
        return Task.CompletedTask;
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
            ApplicationName = "Meet Amalfi Coast"
        });
    }

    public async Task<string> StartWatchAsync()
    {
        CalendarService service = await CreateCalendarServiceAsync();

        await EnsureSyncTokenAsync(service);

        string channelId = Guid.NewGuid().ToString();

        Channel channel = new Channel
        {
            Id = channelId,
            Type = "web_hook",
            Address = _settings.WebhookUrl
        };

        Channel result = await service.Events.Watch(channel, "primary").ExecuteAsync();

        return result.Id;
    }

    public async Task<List<Event>> GetChangedEventsAsync()
    {
        CalendarService service = await CreateCalendarServiceAsync();

        string syncTokenPath = GetSyncTokenPath();

        if (!File.Exists(syncTokenPath))
        {
            await EnsureSyncTokenAsync(service);
            return new List<Event>();
        }

        string syncToken = await File.ReadAllTextAsync(syncTokenPath);

        EventsResource.ListRequest request = service.Events.List("primary");
        request.SyncToken = syncToken;
        request.ShowDeleted = true;

        Events events = await request.ExecuteAsync();

        if (!string.IsNullOrWhiteSpace(events.NextSyncToken))
        {
            await File.WriteAllTextAsync(syncTokenPath, events.NextSyncToken);
        }

        return events.Items?.ToList() ?? new List<Event>();
    }

    private async Task EnsureSyncTokenAsync(CalendarService service)
    {
        string syncTokenPath = GetSyncTokenPath();

        if (File.Exists(syncTokenPath))
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
            await File.WriteAllTextAsync(syncTokenPath, events.NextSyncToken);
        }
    }

    private string GetSyncTokenPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "google-calendar-sync-token.txt");
    }
}