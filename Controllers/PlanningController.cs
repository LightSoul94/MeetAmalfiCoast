using Microsoft.AspNetCore.Mvc;
using MeetAmalfiCoast.Models;
using Stripe;

namespace MeetAmalfiCoast.Controllers;

[Route("Planning")]
public class PlanningController : Controller
{
    private readonly StripeService _stripeService;
    private readonly ILogger<PlanningController> _logger;

    public PlanningController(
        StripeService stripeService,
        ILogger<PlanningController> logger)
    {
        _stripeService = stripeService;
        _logger = logger;
    }

    [HttpPost("CreateCheckoutSession")]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] PlanningAppointment appointment)
    {
        try
        {
            var result = await _stripeService.CreateCheckoutSessionAsync(
                appointment,
                Request);

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

    [HttpPost("StripeWebhook")]
    public async Task<IActionResult> StripeWebhook()
    {
        try
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            Stripe.Event? stripeEvent = _stripeService.ConstructEventFromWebhook(
                json,
                Request.Headers["Stripe-Signature"].ToString());

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
}