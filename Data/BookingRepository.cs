using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MeetingRoomSystem.Models;

namespace MeetingRoomSystem.Data;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<BookingRepository> _logger;

    public BookingRepository(AppDbContext db, ILogger<BookingRepository> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task SaveCameraDetectionAsync(RoomSnapshot detection)
    {
        _db.CameraDetections.Add(detection);
        await _db.SaveChangesAsync();

        _logger.LogDebug(
            "Saved detection for {RoomId}: PersonDetected={Detected}, " + "CameraAvailable={Available}",
            detection.RoomId, detection.PersonDetected, detection.CameraAvailable);
    }

    public async Task<List<RoomSnapshot>> GetRecentDetectionsAsync(
        string roomId, int count = 15)
    {
        return await _db.CameraDetections
            .Where(c => c.RoomId == roomId)
            .OrderByDescending(c => c.CapturedAt)
            .Take(count)
            .OrderBy(c => c.CapturedAt)
            .ToListAsync();
    }

    public async Task UpsertCalendarBookingAsync(Booking booking)
    {
        var existing = await _db.CalendarBookings
            .FirstOrDefaultAsync(b => b.EventId == booking.EventId);

        if (existing == null)
        {
            _db.CalendarBookings.Add(booking);
            _logger.LogDebug("Inserting new booking {EventId}: '{Title}'",
                booking.EventId, booking.Title);
        }
        else
        {
            existing.Title          = booking.Title;
            existing.StartTime      = booking.StartTime;
            existing.EndTime        = booking.EndTime;
            existing.Status         = booking.Status;
            existing.OrganizerEmail = booking.OrganizerEmail;
            existing.OrganizerName  = booking.OrganizerName;
            existing.LastSyncedAt   = DateTime.UtcNow;

            _logger.LogDebug("Updating booking {EventId}: '{Title}'",
                booking.EventId, booking.Title);
        }

        await _db.SaveChangesAsync();
    }

    public async Task<Booking?> GetActiveBookingAsync(string roomId)
    {
        var now = DateTime.UtcNow;
        return await _db.CalendarBookings
            .Where(b => b.RoomId    == roomId
                     && b.StartTime <= now
                     && b.EndTime   >= now
                     && b.Status    == "confirmed")
            .FirstOrDefaultAsync();
    }

    public async Task<List<Booking>> GetBookingsInRangeAsync(
        string roomId, DateTime from, DateTime to)
    {
        return await _db.CalendarBookings
            .Where(b => b.RoomId    == roomId
                     && b.StartTime >= from
                     && b.EndTime   <= to
                     && b.Status    == "confirmed")
            .OrderBy(b => b.StartTime)
            .ToListAsync();
    }

    public async Task SaveDetectionResultAsync(DetectionResult result)
    {
        _db.DetectionResults.Add(result);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Saved detection result for {RoomId}: Status={Status}",
            result.RoomId, result.Status);
    }

    public async Task<DetectionResult?> GetLatestResultAsync(string roomId)
    {
        return await _db.DetectionResults
            .Where(d => d.RoomId == roomId)
            .OrderByDescending(d => d.EvaluatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<DetectionResult>> GetLatestResultsAllRoomsAsync()
    {
        return await _db.DetectionResults
            .GroupBy(d => d.RoomId)
            .Select(g => g.OrderByDescending(d => d.EvaluatedAt).First())
            .ToListAsync();
    }

    public async Task<List<DetectionResult>> GetUnalertedGhostsAsync()
    {
        return await _db.DetectionResults
            .Where(d => d.Status == "Ghost" && !d.AlertSent)
            .ToListAsync();
    }
}