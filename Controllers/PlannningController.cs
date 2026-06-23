using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

public class PlanningController : Controller
{
    private readonly GoogleCalendarService _googleCalendarService;
    private readonly FirestorePlanningService _firestorePlanningService;

    public PlanningController(
    GoogleCalendarService googleCalendarService,
    FirestorePlanningService firestorePlanningService)
    {
        _googleCalendarService = googleCalendarService;
        _firestorePlanningService = firestorePlanningService;
    }

    public ActionResult ConnectGoogleCalendar()
    {
        string authUrl = _googleCalendarService.GetAuthorizationUrl();
        return Redirect(authUrl);
    }

    public async Task<ActionResult> GoogleCalendarCallback(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return RedirectToAction("Planning", "Home");

        await _googleCalendarService.SaveTokenAsync(code);

        return RedirectToAction("Planning", "Home");
    }

    [HttpPost]
    public async Task<JsonResult> SyncGoogleCalendar()
    {
        try
        {
            var pendingAppointments = await _firestorePlanningService.GetPendingAppointmentsAsync();

            foreach (var appointment in pendingAppointments)
            {
                var googleEventId = await _googleCalendarService.CreateEventAsync(appointment);

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
            return Json(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> GoogleCalendarWebhook()
    {
        Console.WriteLine("WEBHOOK GOOGLE RICEVUTO");

        foreach (var header in Request.Headers)
        {
            Console.WriteLine($"{header.Key}: {header.Value}");
        }

        var changedEvents = await _googleCalendarService.GetChangedEventsAsync();

        Console.WriteLine($"Eventi cambiati trovati: {changedEvents.Count}");

        foreach (var googleEvent in changedEvents)
        {
            Console.WriteLine($"Evento: {googleEvent.Id} - Status: {googleEvent.Status}");

            if (googleEvent.Status == "cancelled")
            {
                Console.WriteLine($"Elimino da Firestore evento Google: {googleEvent.Id}");

                await _firestorePlanningService.DeleteAppointmentByGoogleEventIdAsync(
                    googleEvent.Id
                );
            }
        }

        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> StartGoogleCalendarWatch()
    {
        try
        {
            string channelId = await _googleCalendarService.StartWatchAsync();

            return Content("Watch attivato. ChannelId: " + channelId);
        }
        catch (Exception ex)
        {
            return Content("Errore: " + ex.Message);
        }
    }
}