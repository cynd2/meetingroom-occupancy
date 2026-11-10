using Microsoft.Extensions.Logging;
using OpenCvSharp;
using MeetingRoomSystem.Interfaces;

namespace MeetingRoomSystem.Services;

// Captures a frame from the local webcam and returns JPEG bytes.
public class WebcamCameraService : ICameraService
{
    private readonly ILogger<WebcamCameraService> _logger;
    private readonly int _deviceIndex;

    public WebcamCameraService(
        ILogger<WebcamCameraService> logger,
        int deviceIndex = 0)
    {
        _logger      = logger;
        _deviceIndex = deviceIndex;
    }

    public async Task<byte[]> CaptureFrameBytesAsync(string roomId)
    {
        return await Task.Run(() => CaptureBytes(roomId));
    }

    private byte[] CaptureBytes(string roomId)
    {
        _logger.LogDebug(
            "Opening webcam device {Index} for room {RoomId}",
            _deviceIndex, roomId);

        using var capture = new VideoCapture(_deviceIndex);

        if (!capture.IsOpened())
            throw new InvalidOperationException(
                $"Could not open webcam at device index {_deviceIndex}.");

        // Warmup frames — lets camera adjust exposure
        using var warmup = new Mat();
        for (int i = 0; i < 5; i++)
            capture.Read(warmup);

        using var frame = new Mat();
        bool success = capture.Read(frame);

        if (!success || frame.Empty())
            throw new InvalidOperationException(
                "Webcam returned an empty frame.");

        // Encode Mat to JPEG bytes in memory
        Cv2.ImEncode(".jpg", frame, out var bytes);

        _logger.LogInformation(
            "Webcam frame encoded for room {RoomId}: {Bytes} bytes",
            roomId, bytes.Length);

        return bytes;
    }

    public async Task<bool> IsCameraOnlineAsync(string roomId)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var capture = new VideoCapture(_deviceIndex);
                return capture.IsOpened();
            }
            catch { return false; }
        });
    }
}