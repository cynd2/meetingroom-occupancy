namespace MeetingRoomSystem.Models;
public class Booking
{
    // Google calendar's unique ID for this event
    // Used to find, cancel or update the event via API
    
    public int Id { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    
    public string OrganizerEmail { get; set; } = string.Empty;
    public string OrganizerName { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;


    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    
    public bool IsActive()
    {
        var now = DateTime.UtcNow;
        return Status == "confirmed"
               && now >= StartTime
               && now <= EndTime;
    }

    public bool IsUpcoming()
    {
        return Status == "confirmed" && StartTime > DateTime.UtcNow;
    }


    public bool IsCancelled()
    {
        return Status == "cancelled";
    }
        

    // How long is this booking
    public int DurationMinutes()
    {
        return (int)(EndTime - StartTime).TotalMinutes;
    }
    
    // Format for display
    public string GetTimeRange()
    {
        return $"{StartTime:HH:mm} – {EndTime:HH:mm}";
    } 
}