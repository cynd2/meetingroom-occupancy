using Microsoft.EntityFrameworkCore;
using MeetingRoomSystem.Models;
using MeetingRoomSystem.Data;
using MeetingRoomSystem.Interfaces;
using MeetingRoomSystem.Services;
using MeetingRoomSystem.Workers;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

var builder = WebApplication.CreateBuilder(args);

var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "meetingrooms.db");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"),
    ServiceLifetime.Scoped);

builder.Services.AddScoped<IBookingRepository, BookingRepository>();

// ── Detection
builder.Services.Configure<DetectionSettings>(opts =>
{
    opts.ModelPath           = "Models/ssd_mobilenet_v1_12.onnx";
    opts.ConfidenceThreshold = 0.5f;
    opts.PersonClassId       = 1;
    opts.ModelInputSize      = 300;
});
builder.Services.AddSingleton<IDetectionService, SsdDetectionService>();

// Camera — MockCameraService (switch to UnifiCameraService once NVR IP + token are ready)
builder.Services.AddScoped<ICameraService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<MockCameraService>>();
    return new MockCameraService(logger, testImagesPath: "TestImages");
});

// Calendar
builder.Services.AddScoped<ICalendarService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<GoogleCalendarService>>();
    var calendarApiService = GoogleCalendarAuth.CreateAsync()
        .GetAwaiter().GetResult();
    return new GoogleCalendarService(calendarApiService, logger);
});

// SnapshotService
builder.Services.Configure<SnapshotSettings>(opts =>
{
    opts.SaveAuditImages     = false;
    opts.AuditImageDirectory = "AuditImages";
});
builder.Services.AddScoped<SnapshotService>();

// Background Workers
builder.Services.AddHostedService<CameraWorker>();
builder.Services.AddHostedService<CalendarSyncWorker>();

// Web API
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase);

builder.Logging.AddConsole()
    .SetMinimumLevel(MsLogLevel.Information)
    .AddFilter("Microsoft.EntityFrameworkCore", MsLogLevel.Warning);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    // Reseed rooms if the count is wrong, names are placeholder, or CameraRoomId isn't a URL yet
    var existingRooms = await db.MeetingRooms.ToListAsync();
    if (existingRooms.Count != 16 || existingRooms.Any(r => r.Name == "Room A")
        || existingRooms.Any(r => r.CameraRoomId != null && !r.CameraRoomId.StartsWith("http")))
    {
        db.MeetingRooms.RemoveRange(existingRooms);
        await db.SaveChangesAsync();

        db.MeetingRooms.AddRange(
            // Ground Floor (1 room)
            new MeetingRoom
            {
                Name           = "A",
                Location       = "Ground Floor",
                Capacity       = 12,
                ResourceEmail  = "",
                CameraRoomId   = "",
                CalendarRoomId = "",
            },

            // 1st Floor (4 rooms)
            new MeetingRoom
            {
                Name           = "B",
                Location       = "1st Floor",
                Capacity       = 0,
                ResourceEmail  = "",
                CameraRoomId   = "",
                CalendarRoomId = "",
            },
            new MeetingRoom
            {
                Name           = "C",
                Location       = "1st Floor",
                Capacity       = 0,
                ResourceEmail  = "",
                CameraRoomId   = "",
                CalendarRoomId = "",
            },
            new MeetingRoom
            {
                Name           = "D",
                Location       = "1st Floor",
                Capacity       = 0,
                ResourceEmail  = "",
                CameraRoomId   = "",
                CalendarRoomId = "",
            },
            new MeetingRoom
            {
                Name           = "E",
                Location       = "1st Floor",
                Capacity       = 0,
                ResourceEmail  = "",
                CameraRoomId   = "",
                CalendarRoomId = "",
            },

            // 2nd Floor (11 rooms)
            new MeetingRoom
            {
                Name           = "F",
                Location       = "2nd Floor",
                Capacity       = 4,
                ResourceEmail  = "",
                CameraRoomId   = "",
                CalendarRoomId = "",
            },
            new MeetingRoom
            {
                Name           = "G",
                Location       = "2nd Floor",
                Capacity       = 4,
                ResourceEmail  = "",
                CameraRoomId   = "",
                CalendarRoomId = "",
            },
            new MeetingRoom
            {
                Name           = "H",
                Location       = "2nd Floor",
                Capacity       = 4,
                ResourceEmail  = "",
                CameraRoomId   = "",
                CalendarRoomId = "",
            },
            new MeetingRoom
            {
                Name           = "I",
                Location       = "2nd Floor",
                Capacity       = 4,
                ResourceEmail  = "",
                CameraRoomId   = "",
                CalendarRoomId = "",
            },
            new MeetingRoom
            {
                Name           = "J",
                Location       = "2nd Floor",
                Capacity       = 3,
                ResourceEmail  = "",
                CameraRoomId   = "",
                CalendarRoomId = "",
            },
            new MeetingRoom
            {
                Name           = "K",
                Location       = "2nd Floor",
                Capacity       = 4,
                ResourceEmail  = "",
                CameraRoomId   = null,
                CalendarRoomId = "",
            },
            new MeetingRoom
            {
                Name           = "L",
                Location       = "2nd Floor",
                Capacity       = 3,
                ResourceEmail  = "",
                CameraRoomId   = null,
                CalendarRoomId = "",
            },
            new MeetingRoom
            {
                Name           = "M",
                Location       = "2nd Floor",
                Capacity       = 3,
                ResourceEmail  = "",
                CameraRoomId   = null,
                CalendarRoomId = "",
            },
            new MeetingRoom
            {
                Name           = "N",
                Location       = "2nd Floor",
                Capacity       = 3,
                ResourceEmail  = "",
                CameraRoomId   = null,
                CalendarRoomId = "",
            },
            new MeetingRoom
            {
                Name           = "O",
                Location       = "2nd Floor",
                Capacity       = 4,
                ResourceEmail  = "",
                CameraRoomId   = "",
                CalendarRoomId = "",
            },
            new MeetingRoom
            {
                Name           = "P",
                Location       = "2nd Floor",
                Capacity       = 10,
                ResourceEmail  = "",
                CameraRoomId   = "",
                CalendarRoomId = "",
            }
        );
        await db.SaveChangesAsync();
        Console.WriteLine("Seeded 16 rooms (Ground + 1st + 2nd Floor).");
    }

    Console.WriteLine("Database ready.");
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

await app.RunAsync();
