using Microsoft.AspNetCore.Mvc;

namespace MeetAmalfiCoasts.Controllers;

public class PlanningController : Controller
{
    private readonly GoogleCalendarService _googleCalendarService;
    private readonly FirestorePlanningService _firestorePlanningService;
    private readonly ILogger<PlanningController> _logger;

    public PlanningController(
        GoogleCalendarService googleCalendarService,
        FirestorePlanningService firestorePlanningService,
        ILogger<PlanningController> logger)
    {
        _googleCalendarService = googleCalendarService;
        _firestorePlanningService = firestorePlanningService;
        _logger = logger;
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
            return RedirectToAction("Planning", "Home");
        }

        try
        {
            await _googleCalendarService.SaveTokenAsync(code);

            return RedirectToAction("Planning", "Home");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il collegamento a Google Calendar");

            return RedirectToAction("Planning", "Home");
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
}