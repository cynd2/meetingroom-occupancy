namespace MeetingRoomSystem.Models;

public class GhostBooking
{
    public int Id { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string OrganizerEmail { get; set; } = string.Empty;
    public DateTime BookingStartTime { get; set; }
    public DateTime BookingEndTime { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public bool AlertSent { get; set; } = false;
    public DateTime? AlertSentAt { get; set; }
    public int EmptyForMinutes { get; set; }

    public bool IsStillActive()
        => BookingEndTime > DateTime.UtcNow;

    public bool CanSendAlert()
        => !AlertSent && IsStillActive();

    public void MarkAlertSent()
    {
        AlertSent = true;
        AlertSentAt = DateTime.UtcNow;
    }
}