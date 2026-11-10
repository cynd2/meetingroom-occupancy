using MeetingRoomSystem.Models;

namespace MeetingRoomSystem.Interfaces;

public interface ICalendarService
{
    Task<List<Booking>> GetBookingsInRangeAsync(string roomId, DateTime from, DateTime to);
    Task<List<Booking>> GetActiveBookingsAsync(string roomId);
}