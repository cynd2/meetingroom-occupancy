using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MeetingRoomSystem.Data;
using MeetingRoomSystem.Models;

namespace MeetingRoomSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly AppDbContext _db;
    private static readonly TimeSpan Wib = TimeSpan.FromHours(7);

    public RoomsController(AppDbContext db) => _db = db;

    // GET /api/rooms
    [HttpGet]
    public async Task<IActionResult> GetAllRooms()
    {
        var now = DateTime.UtcNow;
        
        var rooms = await _db.MeetingRooms
            .Include(r => r.CurrentBooking)
            .OrderBy(r => r.Location)
            .ThenBy(r => r.Name)
            .ToListAsync();

        List<string> cameraIds = [..rooms.Select(r => r.CameraRoomId).Where(id => !string.IsNullOrEmpty(id))];
        List<string> calIds    = [..rooms.Select(r => r.CalendarRoomId).Where(id => !string.IsNullOrEmpty(id))];

        // Latest anomaly minutes per room from DetectionResults
        var latestResults = await _db.DetectionResults
            .Where(d => cameraIds.Contains(d.RoomId))
            .GroupBy(d => d.RoomId)
            .Select(g => g.OrderByDescending(d => d.EvaluatedAt).First())
            .ToListAsync();
        var resultByCameraId = latestResults.ToDictionary(r => r.RoomId);

        // Next upcoming bookings per room
        var upcomingBookings = await _db.CalendarBookings
            .Where(b => calIds.Contains(b.RoomId)
                     && b.StartTime > now
                     && b.Status == "confirmed")
            .OrderBy(b => b.StartTime)
            .ToListAsync();
        var nextBookingByCalId = upcomingBookings
            .GroupBy(b => b.RoomId)
            .ToDictionary(g => g.Key, g => g.First());

        // Load up to 60 min of snapshots — last 15 used for chart, all used for anomaly counting
        var cutoff = now.AddMinutes(-60);
        var snapshots = await _db.CameraDetections
            .Where(s => cameraIds.Contains(s.RoomId) && s.CapturedAt >= cutoff)
            .OrderBy(s => s.CapturedAt)
            .ToListAsync();
        var snapshotsByCameraId = snapshots
            .GroupBy(s => s.RoomId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Assemble per-room DTOs
        var roomDtos = rooms.Select(r =>
        {
            // Status 
            var hasCameraData = r.LastCheckedAt.HasValue;
            var status = hasCameraData
                ? r.GetStatus()
                : (r.IsCurrentlyBooked() ? "Occupied" : "Free");

            // Anomaly minutes — how long has this room been in the anomaly state?
            var allSnaps = !string.IsNullOrEmpty(r.CameraRoomId)
                ? snapshotsByCameraId.GetValueOrDefault(r.CameraRoomId, [])
                : [];

            var anomalyMinutes = status switch
            {
                "Ghost"    => CalcAnomalyMinutes(allSnaps, wantPerson: false, now),
                "Squatter" => CalcAnomalyMinutes(allSnaps, wantPerson: true,  now),
                _          => 0,
            };

            // Current booking display info
            object? currentBooking = null;
            if (r.CurrentBooking is { } cb)
            {
                var startsAtWib = cb.StartTime.Add(Wib);
                var endsAtWib   = cb.EndTime.Add(Wib);
                var minsLeft    = Math.Max(0, (int)(cb.EndTime - now).TotalMinutes);
                currentBooking = new
                {
                    bookedBy = string.IsNullOrEmpty(cb.OrganizerName)
                        ? cb.OrganizerEmail
                        : cb.OrganizerName,
                    startsAt  = startsAtWib.ToString("HH:mm"),
                    endsAt    = endsAtWib.ToString("HH:mm"),
                    minsLeft,
                };
            }

            // Next upcoming booking
            object? nextBooking = null;
            if (!string.IsNullOrEmpty(r.CalendarRoomId)
                && nextBookingByCalId.TryGetValue(r.CalendarRoomId, out var nb))
            {
                var startWib = nb.StartTime.Add(Wib);
                var endWib   = nb.EndTime.Add(Wib);
                var minsLeft = Math.Max(0, (int)(nb.StartTime - now).TotalMinutes);
                nextBooking = new
                {
                    range    = $"{startWib:HH:mm} – {endWib:HH:mm}",
                    beginAt  = startWib.ToString("HH:mm"),
                    minsLeft,
                };
            }

            // "Last checked X min ago" for free rooms with camera
            string? lastChecked = null;
            if (hasCameraData && r.LastCheckedAt.HasValue)
            {
                var agoMins = (int)(now - r.LastCheckedAt.Value).TotalMinutes;
                lastChecked = agoMins <= 1 ? "just now" : $"{agoMins} min ago";
            }

            // Activity chart — last 15 minutes of snapshots
            var snaps = allSnaps.Where(s => s.CapturedAt >= now.AddMinutes(-15)).ToList();

            var activity = snaps.Select(s => new
            {
                t = s.CapturedAt.Add(Wib).ToString("HH.mm"),
                v = s.PersonDetected ? 1 : 0,
            }).ToList();

            // 4 evenly-spaced time labels across the activity window
            string[] timeLabels = BuildTimeLabels(snaps);

            return new
            {
                id             = r.Id,
                name           = r.Name,
                floor          = r.Location,
                seats          = r.Capacity,
                email          = r.ResourceEmail,
                status         = status.ToLower(),
                lastChecked,
                currentBooking,
                nextBooking,
                emptyForMins   = status == "Ghost"    ? (int?)anomalyMinutes : null,
                occupiedForMins= status == "Squatter" ? (int?)anomalyMinutes : null,
                activity,
                timeLabels,
                calendarId     = r.CalendarRoomId,
            };
        }).ToList();
        
        static int StatusOrder(string s) => s switch
        {
            "free"     => 0,
            "ghost"    => 1,
            "occupied" => 2,
            "squatter" => 3,
            _          => 4,
        };

        var floors = roomDtos
            .GroupBy(r => r.floor)
            .Select(g => new
            {
                label = g.Key,
                rooms = g.OrderBy(r => StatusOrder(r.status)).ThenBy(r => r.name).ToList(),
            })
            .ToList();
        
        var summary = new
        {
            total    = roomDtos.Count,
            free     = roomDtos.Count(r => r.status == "free"),
            occupied = roomDtos.Count(r => r.status == "occupied" || r.status == "ghost" || r.status == "squatter"),
        };

        return Ok(new { summary, floors });
    }

    // GET /api/rooms/debug — temporary: shows raw anomaly calculation data per room
    [HttpGet("debug")]
    public async Task<IActionResult> GetDebug()
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddMinutes(-60);

        var rooms = await _db.MeetingRooms
            .Include(r => r.CurrentBooking)
            .ToListAsync();

        var cameraIds = rooms
            .Where(r => !string.IsNullOrEmpty(r.CameraRoomId))
            .Select(r => r.CameraRoomId!)
            .ToList();

        var snapshots = await _db.CameraDetections
            .Where(s => cameraIds.Contains(s.RoomId) && s.CapturedAt >= cutoff)
            .OrderBy(s => s.CapturedAt)
            .ToListAsync();

        var snapshotsByCameraId = snapshots
            .GroupBy(s => s.RoomId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = rooms.Select(r =>
        {
            var hasCameraData = r.LastCheckedAt.HasValue;
            var status = hasCameraData
                ? r.GetStatus()
                : (r.IsCurrentlyBooked() ? "Occupied" : "Free");

            var allSnaps = !string.IsNullOrEmpty(r.CameraRoomId)
                ? snapshotsByCameraId.GetValueOrDefault(r.CameraRoomId, [])
                : [];

            var anomalyMinutes = status switch
            {
                "Ghost"    => CalcAnomalyMinutes(allSnaps, wantPerson: false, now),
                "Squatter" => CalcAnomalyMinutes(allSnaps, wantPerson: true,  now),
                _          => 0,
            };

            var lastSnap = allSnaps.LastOrDefault();

            return new
            {
                name            = r.Name,
                status,
                isOccupied      = r.IsOccupied,
                isBooked        = r.IsCurrentlyBooked(),
                bookingId       = r.CurrentBookingEventId,
                bookingActive   = r.CurrentBooking?.IsActive(),
                bookingEnd      = r.CurrentBooking?.EndTime.ToString("HH:mm:ss") + " UTC",
                snapshotCount   = allSnaps.Count,
                anomalyMinutes,
                lastSnapPersonDetected  = lastSnap?.PersonDetected,
                lastSnapCameraAvailable = lastSnap?.CameraAvailable,
                lastSnapTime    = lastSnap?.CapturedAt.ToString("HH:mm:ss") + " UTC",
                cameraRoomId    = r.CameraRoomId?[^30..], // last 30 chars for readability
            };
        });

        return Ok(result);
    }

    // GET /api/rooms/{id}
    // Returns one room's latest verdict + last 15 camera detections.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetRoom(int id)
    {
        var room = await _db.MeetingRooms
            .Include(r => r.CurrentBooking)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room is null) return NotFound();

        var latestResult = !string.IsNullOrEmpty(room.CameraRoomId)
            ? await _db.DetectionResults
                .Where(d => d.RoomId == room.CameraRoomId)
                .OrderByDescending(d => d.EvaluatedAt)
                .FirstOrDefaultAsync()
            : null;

        var recentSnaps = !string.IsNullOrEmpty(room.CameraRoomId)
            ? await _db.CameraDetections
                .Where(s => s.RoomId == room.CameraRoomId)
                .OrderByDescending(s => s.CapturedAt)
                .Take(15)
                .OrderBy(s => s.CapturedAt)
                .ToListAsync()
            : [];

        return Ok(new
        {
            room = new
            {
                id     = room.Id,
                name   = room.Name,
                floor  = room.Location,
                seats  = room.Capacity,
                email  = room.ResourceEmail,
                status = room.GetStatus().ToLower(),
            },
            latestVerdict = latestResult is null ? null : new
            {
                status         = latestResult.Status,
                evaluatedAt    = latestResult.EvaluatedAt,
                anomalyMinutes = latestResult.AnomalyMinutes,
            },
            activity = recentSnaps.Select(s => new
            {
                t = s.CapturedAt.Add(Wib).ToString("HH.mm"),
                v = s.PersonDetected ? 1 : 0,
            }),
        });
    }

    // Returns how many minutes the room has been in the anomaly state.
    // Walks backward through reliable snapshots, tolerating up to 1 consecutive
    // false reading (SSD model noise) before declaring the state truly changed.
    // Uses actual CapturedAt timestamps rather than counting snapshots,
    // so the result is accurate even when camera cycles aren't perfectly 1 min apart.
    private static int CalcAnomalyMinutes(
        List<RoomSnapshot> snaps, bool wantPerson, DateTime now)
    {
        var reliable = snaps
            .Where(s => s.CameraAvailable)
            .OrderByDescending(s => s.CapturedAt)
            .ToList();

        if (reliable.Count == 0) return 0;

        // Most recent snapshot must already match — otherwise no anomaly yet
        if (reliable[0].PersonDetected != wantPerson) return 0;

        // Walk backward; allow at most 1 consecutive mismatch (sensor noise)
        int lastMatchIndex = 0;
        int consecMismatches = 0;

        for (int i = 0; i < reliable.Count; i++)
        {
            if (reliable[i].PersonDetected == wantPerson)
            {
                lastMatchIndex = i;
                consecMismatches = 0;
            }
            else
            {
                if (++consecMismatches > 1) break; // real state change
            }
        }

        return (int)(now - reliable[lastMatchIndex].CapturedAt).TotalMinutes;
    }

    private static string[] BuildTimeLabels(List<RoomSnapshot> snaps)
    {
        if (snaps.Count == 0)
            return [];

        var times = snaps.Select(s => s.CapturedAt.Add(Wib)).ToList();
        if (times.Count == 1)
            return [times[0].ToString("HH.mm")];

        var first  = times.First();
        var last   = times.Last();
        var span   = (last - first).TotalMinutes;
        var q1     = first.AddMinutes(span / 3);
        var q2     = first.AddMinutes(span * 2 / 3);

        return
        [
            first.ToString("HH.mm"),
            q1.ToString("HH.mm"),
            q2.ToString("HH.mm"),
            last.ToString("HH.mm"),
        ];
    }
}
