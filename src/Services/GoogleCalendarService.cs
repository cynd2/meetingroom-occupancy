using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Util;
using MeetingRoomSystem.Interfaces;
using MeetingRoomSystem.Models;
using Microsoft.Extensions.Logging;

namespace MeetingRoomSystem.Services;


public class GoogleCalendarService : ICalendarService
{
    private readonly CalendarService _calendarService;
    private readonly ILogger<GoogleCalendarService> _logger;
    
    public GoogleCalendarService(CalendarService calendarService, ILogger<GoogleCalendarService> logger)
    {
        _calendarService = calendarService;
        _logger          = logger;
    }
    
    public async Task<List<Booking>> GetActiveBookingsAsync(string roomId)
    {
        _logger.LogInformation("Fetching active bookings for room: {RoomId}", roomId);

        try
        {
            var request = _calendarService.Events.List(roomId);
            request.TimeMinDateTimeOffset = DateTimeOffset.UtcNow;
            request.TimeMaxDateTimeOffset = DateTimeOffset.UtcNow.AddHours(8);
            request.SingleEvents          = true;
            request.ShowDeleted           = false;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            // Execute the HTTP call to Google's servers
            var response = await request.ExecuteAsync();

            // Map each Google Event to Booking model
            var bookings = new List<Booking>();

            if (response.Items != null)
            {
                foreach (var googleEvent in response.Items)
                {
                    var booking = MapToBooking(googleEvent, roomId);

                    // Only include confirmed bookings — skip cancelled ones
                    if (!booking.IsCancelled())
                        bookings.Add(booking);
                }
            }

            _logger.LogInformation(
                "Found {Count} active booking(s) for room {RoomId}",
                bookings.Count, roomId);

            return bookings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error fetching bookings for room {RoomId}", roomId);

            // Return empty list
            return new List<Booking>();
        }
    }

    // GetBookingsInRangeAsync
    public async Task<List<Booking>> GetBookingsInRangeAsync(
        string roomId,
        DateTime from,
        DateTime to)
    {
        _logger.LogInformation(
            "Fetching bookings for room {RoomId} from {From} to {To}",
            roomId, from, to);

        try
        {
            var request = _calendarService.Events.List(roomId);
            request.TimeMinDateTimeOffset = new DateTimeOffset(from, TimeSpan.Zero);
            request.TimeMaxDateTimeOffset = new DateTimeOffset(to,   TimeSpan.Zero);
            request.SingleEvents          = true;
            request.ShowDeleted           = false;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            var response = await request.ExecuteAsync();

            var bookings = new List<Booking>();
            if (response.Items != null)
            {
                foreach (var googleEvent in response.Items)
                    bookings.Add(MapToBooking(googleEvent, roomId));
            }

            return bookings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error fetching bookings in range for room {RoomId}", roomId);
            return new List<Booking>();
        }
    }

    private static Booking MapToBooking(Event googleEvent, string roomId)
    {
        var startUtc = googleEvent.Start.DateTimeDateTimeOffset?.UtcDateTime
                    ?? ParseAllDayDate(googleEvent.Start.Date);

        var endUtc   = googleEvent.End.DateTimeDateTimeOffset?.UtcDateTime
                    ?? ParseAllDayDate(googleEvent.End.Date);

        return new Booking
        {
            EventId        = googleEvent.Id ?? string.Empty,

            RoomId       = roomId,

            Title          = googleEvent.Summary ?? "(No title)",

            StartTime      = startUtc,
            EndTime        = endUtc,

            OrganizerEmail = googleEvent.Organizer?.Email        ?? "unknown",
            OrganizerName  = googleEvent.Organizer?.DisplayName  ?? "unknown",

            Status         = googleEvent.Status ?? "confirmed",

            CreatedAt      = DateTime.UtcNow
        };
    }

    private static DateTime ParseAllDayDate(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString))
            return DateTime.MinValue;

        return DateTime.TryParse(dateString, out var date)
            ? DateTime.SpecifyKind(date, DateTimeKind.Utc)
            : DateTime.MinValue;
    }
}