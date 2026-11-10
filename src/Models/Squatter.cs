namespace MeetingRoomSystem.Models;

public class Squatter
{
    public int Id { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public int OccupiedForMinutes { get; set; }
    public DateTime? ResolvedAt { get; set; }
    

    public bool IsOngoing()
        => ResolvedAt == null;

    public void MarkResolved()
        => ResolvedAt = DateTime.UtcNow;

    public int TotalDurationMinutes()
    {
        var end = ResolvedAt ?? DateTime.UtcNow;
        return (int)(end - DetectedAt).TotalMinutes;
    }
}