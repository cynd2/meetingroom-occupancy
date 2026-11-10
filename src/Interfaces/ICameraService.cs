using MeetingRoomSystem.Interfaces;

namespace MeetingRoomSystem.Interfaces;

public interface ICameraService
{
    Task<byte[]> CaptureFrameBytesAsync(string roomId);
    Task<bool> IsCameraOnlineAsync(string roomId);
}