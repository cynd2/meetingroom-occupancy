namespace MeetingRoomSystem.Interfaces;

public interface IDetectionService
{
    Task<bool> IsPersonDetectedAsync(byte[] imageBytes);
    Task<int> CountPersonsAsync(byte[] imageBytes);

    // Returns the highest person confidence score found in the image (0.0 to 1.0).
    Task<float> GetMaxPersonConfidenceAsync(byte[] imageBytes);
    
    // roiPolygon — list of (X, Y) vertices in normalized coordinates (0.0 to 1.0).
    //              X=0 is the left edge, X=1 is the right edge of the frame.
    //              Y=0 is the top edge,  Y=1 is the bottom edge of the frame.
    //              Vertices must be in order (clockwise or counter-clockwise).
    Task<float> GetMaxPersonConfidenceInRoiAsync(
        byte[] imageBytes,
        IReadOnlyList<(float X, float Y)> roiPolygon);
}