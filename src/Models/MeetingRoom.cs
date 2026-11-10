namespace MeetingRoomSystem.Models;

public class MeetingRoom
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string ResourceEmail { get; set; } = string.Empty;

    // Written by CameraWorker after each detection cycle
    // Null = no camera assigned yet
    public string? CameraRoomId { get; set; }
    public bool IsOccupied { get; set; }
    public DateTime? LastCheckedAt { get; set; }

    // Written by CalendarSyncWorker after each sync cycle
    // Null = no calendar resource assigned yet
    public string? CalendarRoomId { get; set; }
    public string? CurrentBookingEventId { get; set; }    // FK → Booking.EventId
    public Booking? CurrentBooking { get; set; }           // EF Core navigation property

    public bool IsCurrentlyBooked()
        => CurrentBooking != null && CurrentBooking.IsActive();

    public bool IsGhostSituation()
        => IsCurrentlyBooked() && !IsOccupied;

    public bool IsSquatterSituation()
        => !IsCurrentlyBooked() && IsOccupied;

    public string GetStatus()
    {
        if (IsGhostSituation())    return "Ghost";
        if (IsSquatterSituation()) return "Squatter";
        if (IsCurrentlyBooked())   return "Occupied";
        return "Free";
    }
}
