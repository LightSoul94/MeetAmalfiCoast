using System.Text.Json.Serialization;

namespace MeetAmalfiCoast.Models;

public class PlanningAppointmentsModel
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("customerName")]
    public string Customer { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public string? CustomerPhone { get; set; }

    public string? PickupAddress { get; set; }

    public string? DropoffAddress { get; set; }

    public string IsoDate { get; set; } = string.Empty;

    public string Start { get; set; } = string.Empty;

    public string End { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public string Status { get; set; } = "confirmed";

    public string? PaymentStatus { get; set; }

    public string? PaymentType { get; set; }

    public long DepositAmount { get; set; }

    public string Currency { get; set; } = "eur";

    public string? StripeSessionId { get; set; }

    public string SyncStatus { get; set; } = "pending";

    public string? SyncError { get; set; }

    public string Source { get; set; } = "website";
}