using Microsoft.Extensions.Logging;
using MeetingRoomSystem.Interfaces;

namespace MeetingRoomSystem.Services;

// Fetches a real JPEG snapshot from any HTTP/HTTPS URL.
public class MockCameraService : ICameraService
{
    private static readonly HttpClient _http = new(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
    })
    {
        Timeout = TimeSpan.FromSeconds(10),
    };

    private readonly ILogger<MockCameraService> _logger;

    public MockCameraService(ILogger<MockCameraService> logger, string testImagesPath = "TestImages")
    {
        _logger = logger;
    }

    public async Task<byte[]> CaptureFrameBytesAsync(string roomId)
    {
        _logger.LogInformation("Fetching snapshot: {Url}", roomId);

        var bytes = await _http.GetByteArrayAsync(roomId);

        _logger.LogInformation("Snapshot received: {Bytes} bytes", bytes.Length);
        return bytes;
    }

    public Task<bool> IsCameraOnlineAsync(string roomId)
    {
        _logger.LogInformation("[MOCK] Camera health check for {RoomId}: online", roomId);
        return Task.FromResult(true);
    }
}
