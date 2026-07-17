namespace MeetAmalfiCoast.Models;
using System.Text.Json.Serialization;

public class PlanningAppointmentModel
{
    public string Id { get; set; } = string.Empty;
    public string IsoDate { get; set; } = string.Empty;
    public string Start { get; set; } = string.Empty;
    public string End { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

   [JsonPropertyName("customerName")]
    public string Customer { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;

    public string? GoogleEventId { get; set; }
    public string? GoogleCalendarId { get; set; }
    public string SyncStatus { get; set; } = "pending";
    public string? SyncError { get; set; }
    public string Source { get; set; } = "manual";
}