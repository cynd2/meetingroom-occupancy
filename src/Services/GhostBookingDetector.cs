using MeetingRoomSystem.Models;
using Microsoft.Extensions.Logging;

namespace MeetingRoomSystem.Services;

public class GhostBookingDetector
{
    private readonly int _consecutiveThreshold;
    private readonly ILogger<GhostBookingDetector> _logger;

    public GhostBookingDetector(
        ILogger<GhostBookingDetector> logger,
        int consecutiveThreshold = 10)
    {
        _logger               = logger;
        _consecutiveThreshold = consecutiveThreshold;
    }

    // CheckForGhost
    public GhostBooking? CheckForGhost(
        string roomId,
        Booking activeBooking,
        List<RoomSnapshot> recentSnapshots)
    {
        var reliable = recentSnapshots
            .Where(s => s.CameraAvailable)
            .ToList();

        if (reliable.Count < _consecutiveThreshold)
        {
            _logger.LogDebug(
                "Ghost check {Room}: {Count}/{Threshold} reliable snapshots — " +
                "not enough data yet",
                roomId, reliable.Count, _consecutiveThreshold);
            return null;
        }

        var latest = reliable
            .OrderByDescending(s => s.Timestamp)
            .Take(_consecutiveThreshold)
            .ToList();

        // All N snapshots must show nobody detected
        if (!latest.All(s => !s.PersonDetected))
        {
            _logger.LogDebug(
                "Ghost check {Room}: person was detected in recent snapshots",
                roomId);
            return null;
        }

        int emptyMinutes = (int)(DateTime.UtcNow - latest.Last().Timestamp)
                           .TotalMinutes;

        _logger.LogWarning(
            "Ghost confirmed — {Room}, booked by {Email}, empty ~{Min} min",
            roomId, activeBooking.OrganizerEmail, emptyMinutes);

        return new GhostBooking
        {
            EventId          = activeBooking.EventId,
            RoomName         = roomId,
            OrganizerEmail   = activeBooking.OrganizerEmail,
            BookingStartTime = activeBooking.StartTime,
            BookingEndTime   = activeBooking.EndTime,
            DetectedAt       = DateTime.UtcNow,
            EmptyForMinutes  = emptyMinutes
        };
    }

    // CheckForSquatter
    public Squatter? CheckForSquatter(
        string roomId,
        List<RoomSnapshot> recentSnapshots)
    {
        var reliable = recentSnapshots
            .Where(s => s.CameraAvailable)
            .ToList();

        if (reliable.Count < _consecutiveThreshold)
        {
            _logger.LogDebug(
                "Squatter check {Room}: {Count}/{Threshold} reliable snapshots — " +
                "not enough data yet",
                roomId, reliable.Count, _consecutiveThreshold);
            return null;
        }

        var latest = reliable
            .OrderByDescending(s => s.Timestamp)
            .Take(_consecutiveThreshold)
            .ToList();

        // All N snapshots must show someone detected
        if (!latest.All(s => s.PersonDetected))
        {
            _logger.LogDebug(
                "Squatter check {Room}: room was empty in recent snapshots",
                roomId);
            return null;
        }

        int occupiedMinutes = (int)(DateTime.UtcNow - latest.Last().Timestamp)
                              .TotalMinutes;

        // Log as Information not Warning — no urgency, no alert fires
        _logger.LogInformation(
            "Squatter detected — {Room}, no booking, occupied ~{Min} min " +
            "(record only, no alert sent)",
            roomId, occupiedMinutes);

        return new Squatter
        {
            RoomName           = roomId,
            DetectedAt         = DateTime.UtcNow,
            OccupiedForMinutes = occupiedMinutes
        };
    }
}