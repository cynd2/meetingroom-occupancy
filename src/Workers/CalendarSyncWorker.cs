using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MeetingRoomSystem.Data;
using MeetingRoomSystem.Interfaces;
using MeetingRoomSystem.Models;

namespace MeetingRoomSystem.Workers;

public class CalendarSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CalendarSyncWorker> _logger;

 
    private readonly List<string> _roomIds = new()
    {
        "" // Fill with Google Calendar ID. for each meeting room
    };

    private readonly TimeSpan _interval = TimeSpan.FromMinutes(10);

    public CalendarSyncWorker(IServiceScopeFactory scopeFactory, ILogger<CalendarSyncWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CalendarSyncWorker started — {Count} room(s), every {Min} min", _roomIds.Count, _interval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("CalendarSyncWorker syncing at {Time} WIB", DateTime.UtcNow.AddHours(7).ToString("HH:mm:ss"));

            foreach (var roomId in _roomIds)
            {
                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    using var scope     = _scopeFactory.CreateScope();
                    var calendarService = scope.ServiceProvider
                        .GetRequiredService<ICalendarService>();
                    var repository      = scope.ServiceProvider
                        .GetRequiredService<IBookingRepository>();

                    // ICalendarService.GetActiveBookingsAsync returns List<Booking>
                    var bookings = await calendarService
                        .GetActiveBookingsAsync(roomId);

                    _logger.LogInformation(
                        "Room {RoomId}: {Count} active booking(s) from calendar",
                        roomId, bookings.Count);

                    // IBookingRepository.UpsertCalendarBookingAsync takes CalendarBooking
                    // Map Booking → CalendarBooking before saving
                    foreach (var b in bookings)
                    {
                        var calendarBooking = new Booking
                        {
                            EventId        = b.EventId,
                            RoomId         = roomId,
                            Title          = b.Title,
                            StartTime      = b.StartTime,
                            EndTime        = b.EndTime,
                            OrganizerEmail = b.OrganizerEmail,
                            OrganizerName  = b.OrganizerName,
                            Status         = b.Status,
                            LastSyncedAt   = DateTime.UtcNow
                        };

                        await repository.UpsertCalendarBookingAsync(calendarBooking);
                    }

                    // Update MeetingRoom.CurrentBookingEventId so the controller
                    // can do a single .Include() query instead of joining manually.
                    var db = scope.ServiceProvider
                        .GetRequiredService<AppDbContext>();
                    var room = await db.MeetingRooms
                        .FirstOrDefaultAsync(r => r.CalendarRoomId == roomId);
                    if (room != null)
                    {
                        var activeBooking = bookings.FirstOrDefault(b => b.IsActive());
                        room.CurrentBookingEventId = activeBooking?.EventId;
                        await db.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "CalendarSyncWorker error for room {RoomId}", roomId);
                }
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("CalendarSyncWorker stopped.");
    }
}