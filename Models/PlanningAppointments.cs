namespace MeetAmalfiCoasts.Models;

public class PlanningAppointment
{
    public string? Id { get; set; }

    public string? Title { get; set; }

    public string? Customer { get; set; }

    public string? CustomerName { get; set; }

    public string? CustomerEmail { get; set; }

    public string? CustomerPhone { get; set; }

    public string? PickupAddress { get; set; }

    public string? DropoffAddress { get; set; }

    public string? IsoDate { get; set; }

    public string? Start { get; set; }

    public string? End { get; set; }

    public string? Notes { get; set; }

    public string? Status { get; set; }

    public string? PaymentStatus { get; set; }

    public string? PaymentType { get; set; }

    public long DepositAmount { get; set; }

    public string? Currency { get; set; }

    public string? StripeSessionId { get; set; }

    public string? SyncStatus { get; set; }

    public string? SyncError { get; set; }

    public string? Source { get; set; }
}