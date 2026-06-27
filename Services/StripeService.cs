using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

public class StripeService
{
    private readonly StripeSettings _stripeSettings;
    private readonly BookingSettings _bookingSettings;
    private readonly FirestorePlanningService _firestorePlanningService;
    private readonly GoogleCalendarService _googleCalendarService;
    private readonly ILogger<StripeService> _logger;

    public StripeService(
        IOptions<StripeSettings> stripeSettings,
        IOptions<BookingSettings> bookingSettings,
        FirestorePlanningService firestorePlanningService,
        GoogleCalendarService googleCalendarService,
        ILogger<StripeService> logger)
    {
        _stripeSettings = stripeSettings.Value;
        _bookingSettings = bookingSettings.Value;
        _firestorePlanningService = firestorePlanningService;
        _googleCalendarService = googleCalendarService;
        _logger = logger;

        StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
    }

    public async Task<object> CreateCheckoutSessionAsync(PlanningAppointment appointment, HttpRequest request)
    {
        string domain = $"{request.Scheme}://{request.Host}";

        long amountInCents = _bookingSettings.DepositAmount * 100;

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = $"{domain}/planning?payment=success",
            CancelUrl = $"{domain}/planning?payment=cancelled",
            CustomerEmail = appointment.CustomerEmail,

            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = _bookingSettings.Currency,
                        UnitAmount = amountInCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Acconto prenotazione Meet Amalfi Coasts",
                            Description = $"{appointment.Title} - {appointment.IsoDate} {appointment.Start}"
                        }
                    }
                }
            },

            Metadata = new Dictionary<string, string>
            {
                { "title", appointment.Title ?? "" },
                { "customerName", appointment.Customer ?? "" },
                { "customerEmail", appointment.CustomerEmail ?? "" },
                { "isoDate", appointment.IsoDate ?? "" },
                { "start", appointment.Start ?? "" },
                { "end", appointment.End ?? "" },
                { "paymentType", "deposit" },
                { "depositAmount", _bookingSettings.DepositAmount.ToString() },
                { "currency", _bookingSettings.Currency }
            }
        };

        var service = new SessionService();
        Session session = await service.CreateAsync(options);

        return new
        {
            success = true,
            checkoutUrl = session.Url
        };
    }

    public Event? ConstructEventFromWebhook(string json, string stripeSignature)
    {
        try
        {
            return EventUtility.ConstructEvent(
                json,
                stripeSignature,
                _stripeSettings.WebhookSecret
            );
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Webhook Stripe non valido");
            return null;
        }
    }

    public async Task HandleStripeEventAsync(Event stripeEvent)
    {
        if (stripeEvent.Type != EventTypes.CheckoutSessionCompleted)
        {
            return;
        }

        Session session = stripeEvent.Data.Object as Session
            ?? throw new Exception("Sessione Stripe non valida.");

        if (session.PaymentStatus != "paid")
        {
            return;
        }

        PlanningAppointment appointment = BuildAppointmentFromSession(session);

        string appointmentId = await _firestorePlanningService.CreatePaidAppointmentAsync(
            appointment,
            session.Id,
            _bookingSettings.DepositAmount,
            _bookingSettings.Currency
        );

        try
        {
            string googleEventId = await _googleCalendarService.CreateEventAsync(appointment);

            await _firestorePlanningService.MarkAsSyncedAsync(
                appointmentId,
                googleEventId,
                "primary"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Appuntamento pagato ma non sincronizzato con Google Calendar");

            await _firestorePlanningService.MarkAsErrorAsync(
                appointmentId,
                ex.Message
            );
        }
    }

    private PlanningAppointment BuildAppointmentFromSession(Session session)
    {
        Dictionary<string, string> metadata = session.Metadata;

        return new PlanningAppointment
        {
            Title = metadata.GetValueOrDefault("title") ?? "Prenotazione",
            Customer = metadata.GetValueOrDefault("customerName") ?? "",
            CustomerEmail = metadata.GetValueOrDefault("customerEmail") ?? "",
            IsoDate = metadata.GetValueOrDefault("isoDate") ?? "",
            Start = metadata.GetValueOrDefault("start") ?? "",
            End = metadata.GetValueOrDefault("end") ?? "",
            SyncStatus = "pending",
            SyncError = null,
            Source = "website"
        };
    }
}