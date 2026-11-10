using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MeetingRoomSystem.Data;
using MeetingRoomSystem.Interfaces;
using MeetingRoomSystem.Models;

namespace MeetingRoomSystem.Services;


public class SnapshotSettings
{
    public bool SaveAuditImages { get; set; } = false;
    public string AuditImageDirectory { get; set; } = "AuditImages";
}

public class SnapshotService
{
    private readonly ICameraService _camera;
    private readonly IDetectionService _detection;
    private readonly IBookingRepository _repository;
    private readonly SnapshotSettings _settings;
    private readonly ILogger<SnapshotService> _logger;

    public SnapshotService(
        ICameraService camera,
        IDetectionService detection,
        IBookingRepository repository,
        IOptions<SnapshotSettings> settings,
        ILogger<SnapshotService> logger)
    {
        _camera     = camera;
        _detection  = detection;
        _repository = repository;
        _settings   = settings.Value;
        _logger     = logger;
    }

   
    // Number of frames captured per cycle. Person detected if ANY frame detects one.
    private const int FramesPerCycle = 3;
    // Delay between frames so each capture is a distinct snapshot
    private const int FrameDelayMs = 2000;

    public async Task<RoomSnapshot?> ProcessRoomAsync(string roomId)
    {
        _logger.LogInformation(
            "Processing room {RoomId} at {Time} WIB ({Frames} frames)",
            roomId, DateTime.UtcNow.AddHours(7).ToString("HH:mm:ss"), FramesPerCycle);

        // Capture FramesPerCycle frames
        byte[]? bestFrame    = null;
        bool personDetected  = false;
        float confidence     = 0f;
        bool cameraAvailable = false;

        for (int i = 0; i < FramesPerCycle; i++)
        {
            if (i > 0) await Task.Delay(FrameDelayMs);

            byte[] frameBytes;
            try
            {
                frameBytes      = await _camera.CaptureFrameBytesAsync(roomId);
                cameraAvailable = true;
                _logger.LogDebug("Frame {Frame}/{Total} captured for {RoomId}: {Bytes} bytes",
                    i + 1, FramesPerCycle, roomId, frameBytes.Length);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Frame {Frame}/{Total} capture failed for {RoomId}",
                    i + 1, FramesPerCycle, roomId);
                continue;
            }

            bool frameHasPerson;
            try
            {
                frameHasPerson = await _detection.IsPersonDetectedAsync(frameBytes);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Detection failed on frame {Frame} for {RoomId}", i + 1, roomId);
                bestFrame ??= frameBytes;
                continue;
            }

            if (frameHasPerson && !personDetected)
            {
                personDetected = true;
                confidence     = 0.85f;
                bestFrame      = frameBytes; // keep the frame that found a person
            }

            bestFrame ??= frameBytes; // fallback: use first captured frame
        }

        if (!cameraAvailable)
        {
            _logger.LogWarning("All {Total} frames failed for {RoomId} — camera unavailable", FramesPerCycle, roomId);
            var failedDetection = new RoomSnapshot
            {
                RoomId              = roomId,
                CapturedAt          = DateTime.UtcNow,
                FramePath           = string.Empty,
                PersonDetected      = false,
                DetectionConfidence = 0f,
                CameraAvailable     = false,
            };
            await _repository.SaveCameraDetectionAsync(failedDetection);
            return null;
        }

        _logger.LogInformation(
            "Detection for {RoomId}: PersonDetected={Detected} (across {Frames} frames)",
            roomId, personDetected, FramesPerCycle);

        // Save annotated image using the best frame (person frame if found, otherwise last frame)
        string auditImagePath = string.Empty;
        if (_detection is SsdDetectionService ssd && bestFrame != null)
        {
            try
            {
                auditImagePath = await ssd.DrawAndSaveAsync(bestFrame, roomId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Annotated snapshot save failed for {RoomId}", roomId);
            }
        }

        var detection = new RoomSnapshot
        {
            RoomId              = roomId,
            CapturedAt          = DateTime.UtcNow,
            FramePath           = auditImagePath,
            PersonDetected      = personDetected,
            DetectionConfidence = confidence,
            CameraAvailable     = true,
        };

        await _repository.SaveCameraDetectionAsync(detection);

        _logger.LogInformation("Detection saved for {RoomId}: {Status}",
            roomId, personDetected ? "OCCUPIED" : "EMPTY");

        return detection;
    }
}