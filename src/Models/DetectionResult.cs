namespace MeetingRoomSystem.Models;
public class DetectionResult
{
    public int Id { get; set; }

    public string RoomId { get; set; } = string.Empty;
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = string.Empty;
    public string? EventId { get; set; }
    public string? OrganizerEmail { get; set; }
    public int AnomalyMinutes { get; set; }
    public bool AlertSent { get; set; } = false;
    public DateTime? AlertSentAt { get; set; }

    public bool IsGhost()     => Status == "Ghost";
    public bool IsSquatter()  => Status == "Squatter";
    public bool IsOccupied()  => Status == "Occupied";
    public bool IsFree()      => Status == "Free";


    // Readable description for logs and dashboard
    public string GetDescription() => Status switch
    {
        "Ghost"    => $"Ghost in {RoomId} — booked by {OrganizerEmail}, empty {AnomalyMinutes} min",
        "Squatter" => $"Squatter in {RoomId} — no booking, occupied {AnomalyMinutes} min",
        "Occupied" => $"Occupied in {RoomId} — booked by {OrganizerEmail}",
        "Free"     => $"Free — {RoomId} is empty with no booking",
        _          => $"Unknown status for {RoomId}"
    };
}