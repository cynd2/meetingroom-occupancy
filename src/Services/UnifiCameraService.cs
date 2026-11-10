using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MeetingRoomSystem.Interfaces;

namespace MeetingRoomSystem.Services;

// UnifiCameraService : ICameraService
//
// Fetches a JPEG snapshot from Unifi Protect NVR via HTTP GET.
// Returns raw bytes — nothing written to disk.
//
// Unifi Protect snapshot endpoint pattern:
//   GET https://{nvr-host}/proxy/protect/api/cameras/{cameraId}/snapshot
//
// Each room maps to one Unifi camera ID in appsettings.json.

public class UnifiCameraSettings
{
    public string NvrBaseUrl { get; set; } = string.Empty;

    public string ApiToken { get; set; } = string.Empty;

    // Map of roomId → Unifi camera ID
    // e.g. { "room_a": "abc123def456", "room_b": "xyz789" }
    public Dictionary<string, string> CameraIds { get; set; } = new();

    // Skip SSL certificate validation
    public bool IgnoreSslErrors { get; set; } = true;
}

public class UnifiCameraService : ICameraService
{
    private readonly UnifiCameraSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<UnifiCameraService> _logger;

    public UnifiCameraService(
        IOptions<UnifiCameraSettings> settings,
        ILogger<UnifiCameraService> logger)
    {
        _settings = settings.Value;
        _logger   = logger;

        // Configure HttpClient with Unifi auth headers
        // and optional SSL bypass for self-signed NVR certificates
        var handler = new HttpClientHandler();
        if (_settings.IgnoreSslErrors)
            handler.ServerCertificateCustomValidationCallback =
                (_, _, _, _) => true;

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(_settings.NvrBaseUrl),
            Timeout     = TimeSpan.FromSeconds(10)
        };

        // Unifi Protect uses cookie-based auth
        _httpClient.DefaultRequestHeaders.Add(
            "Cookie", $"TOKEN={_settings.ApiToken}");

        // CSRF token required by Unifi Protect API
        // The value can be anything — Unifi just checks it's present
        _httpClient.DefaultRequestHeaders.Add("X-CSRF-Token", "SnapshotService");
    }

    // CaptureFrameBytesAsync
    public async Task<byte[]> CaptureFrameBytesAsync(string roomId)
    {
        // Unifi Protect snapshot endpoint
        var url = roomId;

        _logger.LogDebug(
            "Fetching snapshot from Unifi NVR for room {RoomId}: {Url}",
            roomId, url);

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Unifi snapshot failed for room {RoomId}: HTTP {Status}",
                roomId, (int)response.StatusCode);
            throw new HttpRequestException(
                $"Unifi NVR returned {(int)response.StatusCode} for room '{roomId}'.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync();

        _logger.LogDebug(
            "Snapshot received for room {RoomId}: {Bytes} bytes",
            roomId, bytes.Length);

        return bytes;
    }

    // IsCameraOnlineAsync
    public async Task<bool> IsCameraOnlineAsync(string roomId)
    {
        try
        {
            if (!_settings.CameraIds.TryGetValue(roomId, out var cameraId))
                return false;

            var url      = $"/proxy/protect/api/cameras/{cameraId}/snapshot";
            var request  = new HttpRequestMessage(HttpMethod.Head, url);
            var response = await _httpClient.SendAsync(request);
            var online   = response.IsSuccessStatusCode;

            _logger.LogInformation(
                "Unifi camera health check for {RoomId}: {Status}",
                roomId, online ? "online" : "offline");

            return online;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Unifi camera health check failed for room {RoomId}", roomId);
            return false;
        }
    }
}