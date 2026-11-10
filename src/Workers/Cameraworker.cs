using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MeetingRoomSystem.Data;
using MeetingRoomSystem.Services;

namespace MeetingRoomSystem.Workers;


public class CameraWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CameraWorker> _logger;
    
    private readonly List<string> _roomIds = new()
    {
        "" //fill with URL to each point security camera's snapshot endpoint 
    };

    private readonly TimeSpan _interval = TimeSpan.FromSeconds(60);

    public CameraWorker(IServiceScopeFactory scopeFactory, ILogger<CameraWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CameraWorker started — {Count} room(s), every {Sec}s", _roomIds.Count, _interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("CameraWorker cycle at {Time} WIB", DateTime.UtcNow.AddHours(7).ToString("HH:mm:ss"));

            foreach (var roomId in _roomIds)
            {
                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    // Fresh scope per room per cycle
                    using var scope = _scopeFactory.CreateScope();
                    var snapshotService = scope.ServiceProvider.
                        GetRequiredService<SnapshotService>();

                    var detection = await snapshotService.ProcessRoomAsync(roomId);

                    if (detection != null)
                    {
                        _logger.LogInformation("Room {RoomId}: PersonDetected={Detected}", roomId, detection.PersonDetected);

                        var db = scope.ServiceProvider
                            .GetRequiredService<AppDbContext>();
                        var room = await db.MeetingRooms
                            .FirstOrDefaultAsync(r => r.CameraRoomId == roomId,
                                stoppingToken);
                        if (room != null)
                        {
                            room.IsOccupied    = detection.PersonDetected;
                            room.LastCheckedAt = DateTime.UtcNow;
                            await db.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // One room failing never stops the other rooms
                    _logger.LogError(ex,
                        "CameraWorker error for room {RoomId}", roomId);
                }
            }

            try
            {
                // Respects Ctrl+C — ends wait immediately on shutdown
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("CameraWorker stopped.");
    }
}