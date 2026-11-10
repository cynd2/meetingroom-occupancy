using MeetingRoomSystem.Models;

namespace MeetingRoomSystem.Data;

public interface IBookingRepository
{
    Task SaveCameraDetectionAsync(RoomSnapshot detection);

    Task<List<RoomSnapshot>> GetRecentDetectionsAsync(
        string roomId, int count = 15);
    
    Task UpsertCalendarBookingAsync(Booking booking);
    Task<Booking?> GetActiveBookingAsync(string roomId);

    Task<List<Booking>> GetBookingsInRangeAsync(string roomId, DateTime from, DateTime to);
    
    Task SaveDetectionResultAsync(DetectionResult result);
    
    Task<DetectionResult?> GetLatestResultAsync(string roomId);

    Task<List<DetectionResult>> GetLatestResultsAllRoomsAsync();

    Task<List<DetectionResult>> GetUnalertedGhostsAsync();
}