using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MeetAmalfiCoast.Models;
using Stripe;

namespace MeetAmalfiCoast.Controllers;

[Route("Planning")]
public class PlanningController : Controller
{
    private readonly StripeService _stripeService;
    private readonly FirestorePlanningService _firestorePlanningService;
    private readonly ILogger<PlanningController> _logger;
    private readonly BookingSettings _bookingSettings;

    public PlanningController(
    IOptions<BookingSettings> bookingSettings,
    StripeService stripeService,
    FirestorePlanningService firestorePlanningService,
    ILogger<PlanningController> logger)
    {
        _bookingSettings = bookingSettings.Value;
        _stripeService = stripeService;
        _firestorePlanningService = firestorePlanningService;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost("StripeWebhook")]
    public async Task<IActionResult> StripeWebhook()
    {
        if (_bookingSettings.BypassStripe)
        {
            _logger.LogInformation("Webhook Stripe ignorato perché BypassStripe è attivo.");
            return Ok();
        }

        try
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            Stripe.Event? stripeEvent = _stripeService.ConstructEventFromWebhook(
                json,
                Request.Headers["Stripe-Signature"].ToString()
            );

            if (stripeEvent is null)
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

    [HttpPost("CreateCheckoutSession")]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] PlanningAppointmentsModel appointment)
    {
        if (_bookingSettings.BypassStripe)
        {
            await _firestorePlanningService.CreatePaidAppointmentAsync(
                appointment,
                $"BYPASS_STRIPE_TEST_{Guid.NewGuid()}",
                _bookingSettings.DepositAmount,
                _bookingSettings.Currency
            );

            return Json(new
            {
                success = true,
                bypassStripe = true
            });
        }

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