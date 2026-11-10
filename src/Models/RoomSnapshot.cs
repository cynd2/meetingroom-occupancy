namespace MeetingRoomSystem.Models;

public class RoomSnapshot
{
    public int Id { get; set; }
    public string RoomId { get; set; } = string.Empty;
    public bool PersonDetected { get; set; }
    public float DetectionConfidence { get; set; }
    public bool CameraAvailable { get; set; }
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
    public string FramePath { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    
    public bool IsReliable(float threshold = 0.5f)
    {
        return CameraAvailable && DetectionConfidence >= threshold;
    }

    public int GetAgeMinutes()
    {
        return (int)(DateTime.UtcNow - Timestamp).TotalMinutes;
    }
}