using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MeetAmalfiCoast.Models;
using MeetAmalfiCoast.Services.Configuration;
using System.Globalization;
using Stripe;

namespace MeetAmalfiCoast.Controllers;

public class PlanningController : Controller
{
    private readonly ApplicationSettings _settings;
    private readonly BookingSettings _bookingSettings;
    private readonly GoogleCalendarService _googleCalendarService;
    private readonly FirestorePlanningService _firestorePlanningService;
    private readonly ILogger<PlanningController> _logger;
    private readonly EmailService _emailService;
    private readonly StripeService _stripeService;

    public PlanningController(
        IOptions<ApplicationSettings> applicationOptions,
        IOptions<BookingSettings> bookingSettings,
        GoogleCalendarService googleCalendarService,
        FirestorePlanningService firestorePlanningService,
        ILogger<PlanningController> logger,
        EmailService emailService,
        StripeService stripeService)
    {
        _settings = applicationOptions.Value;
        _bookingSettings = bookingSettings.Value;
        _googleCalendarService = googleCalendarService;
        _firestorePlanningService = firestorePlanningService;
        _logger = logger;
        _emailService = emailService;
        _stripeService = stripeService;
    }

    [HttpGet]
    [Route("planning")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult ConnectGoogleCalendar()
    {
        var authUrl = _googleCalendarService.GetAuthorizationUrl();

        return Redirect(authUrl);
    }

    [HttpGet]
    public async Task<IActionResult> GoogleCalendarCallback(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return RedirectToAction("Index", "Planning");
        }

        try
        {
            await _googleCalendarService.SaveTokenAsync(code);

            return RedirectToAction("Index", "Planning");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il collegamento a Google Calendar");

            return RedirectToAction("Index", "Planning");
        }
    }

    [HttpPost]
    public async Task<IActionResult> SyncGoogleCalendar()
    {
        try
        {
            var pendingAppointments =
                await _firestorePlanningService.GetPendingAppointmentsAsync();

            foreach (var appointment in pendingAppointments)
            {
                var googleEventId =
                    await _googleCalendarService.CreateEventAsync(appointment);

                await _firestorePlanningService.MarkAsSyncedAsync(
                    appointment.Id,
                    googleEventId,
                    "primary"
                );
            }

            return Json(new
            {
                success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la sincronizzazione Firestore verso Google Calendar");

            return Json(new
            {
                success = false,
                message = "Errore durante la sincronizzazione con Google Calendar."
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ResetPlanningFromGoogleCalendar()
    {
        try
        {
            await _googleCalendarService.ResetPlanningFromGoogleCalendarAsync();

            return Content("Planning resettato e ricaricato da Google Calendar.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore reset planning da Google Calendar");

            return Content("Errore reset planning: " + ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] PlanningAppointmentModel appointment)
    {
        // Controlla la validità della data/ora inserita e blocca le prenotazioni nel passato.
        if (!DateTime.TryParseExact(
        $"{appointment.IsoDate} {appointment.Start}",
        "yyyy-MM-dd HH:mm",
        CultureInfo.InvariantCulture,
        DateTimeStyles.None,
        out DateTime appointmentStart))
        {
            return Json(new
            {
                success = false,
                message = "La data o l'orario della prenotazione non sono validi."
            });
        }

        // Verifica che l'orario di fine della prenotazione sia valido e formattato correttamente.
        if (!DateTime.TryParseExact(
        $"{appointment.IsoDate} {appointment.End}",
        "yyyy-MM-dd HH:mm",
        CultureInfo.InvariantCulture,
        DateTimeStyles.None,
        out DateTime appointmentEnd))
        {
            return Json(new
            {
                success = false,
                message = "L'orario di fine della prenotazione non è valido."
            });
        }

        if (appointmentStart <= DateTime.Now)
        {
            return Json(new
            {
                success = false,
                message = "Non è possibile prenotare una data o un orario già trascorso."
            });
        }

        if (appointmentEnd <= appointmentStart)
        {
            return Json(new
            {
                success = false,
                message = "L'orario di fine deve essere successivo all'orario di inizio."
            });
        }

        // Se il bypass Stripe è attivo, crea l'appuntamento direttamente; altrimenti avvia la sessione di checkout Stripe.
        if (_settings.BypassStripe)
        {
            string appointmentId = await _firestorePlanningService.CreatePaidAppointmentAsync(
                appointment,
                "BYPASS_STRIPE_TEST",
                _bookingSettings.DepositAmount,
                _bookingSettings.Currency
            );

            try
            {
                await _emailService.SendNewBookingNotificationAsync(appointment);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Appuntamento creato, ma email al proprietario non inviata"
                );
            }

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
                _logger.LogError(ex, "Appuntamento test creato ma non sincronizzato con Google Calendar");

                await _firestorePlanningService.MarkAsErrorAsync(
                    appointmentId,
                    ex.Message
                );
            }

            return Json(new
            {
                success = true,
                bypassStripe = true
            });
        }
        else
        {
            try
            {
                var result = await _stripeService.CreateCheckoutSessionAsync(
                    appointment,
                    Request
                );

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la creazione della sessione di checkout Stripe");

                return Json(new
                {
                    success = false,
                    message = "Errore durante la creazione della sessione di checkout."
                });
            }
        }
    }

    [HttpPost]
    public async Task<IActionResult> StripeWebhook()
    {
        try
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            var stripeEvent = _stripeService.ConstructEventFromWebhook(
                json,
                Request.Headers["Stripe-Signature"].ToString()
            );

            if (stripeEvent == null)
            {
                return BadRequest();
            }

            await _stripeService.HandleStripeEventAsync(stripeEvent);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la gestione del webhook Stripe");

            return BadRequest();
        }
    }

    [HttpPost]
    public async Task<IActionResult> CleanupOldAppointments()
    {
        try
        {
            int deletedCount =
                await _firestorePlanningService.DeleteOldAppointmentsAsync();

            return Ok(new
            {
                success = true,
                deletedCount,
                message = $"{deletedCount} appuntamenti eliminati."
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Errore durante la pulizia manuale degli appuntamenti Firestore"
            );

            return StatusCode(500, new
            {
                success = false,
                message = "Errore durante la pulizia degli appuntamenti."
            });
        }
    }
}