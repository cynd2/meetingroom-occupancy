using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using MeetingRoomSystem.Interfaces;

namespace MeetingRoomSystem.Services;

public class DetectionSettings
{
    public string ModelPath { get; set; } = "Models/ssd_mobilenet_v1_12.onnx";
    public float ConfidenceThreshold { get; set; } = 0.5f;
    public int PersonClassId { get; set; } = 1;
    public int ModelInputSize { get; set; } = 300;
}

public class SsdDetectionService : IDetectionService
{
    private readonly DetectionSettings _settings;
    private readonly ILogger<SsdDetectionService> _logger;
    private readonly InferenceSession _session;
    private readonly string _inputNodeName;

    public SsdDetectionService(
        IOptions<DetectionSettings> settings,
        ILogger<SsdDetectionService> logger)
    {
        _settings = settings.Value;
        _logger   = logger;

        if (!File.Exists(_settings.ModelPath))
            throw new FileNotFoundException(
                $"ONNX model not found at '{_settings.ModelPath}'.");

        _logger.LogInformation(
            "Loading SSD MobileNetV1 model from {Path}", _settings.ModelPath);

        _session       = new InferenceSession(_settings.ModelPath);
        _inputNodeName = _session.InputMetadata.Keys.First();

        _logger.LogInformation(
            "Detection model loaded. Input node: '{NodeName}'", _inputNodeName);
    }

    public static SsdDetectionService CreateDirect(string modelPath, float threshold = 0.5f)
    {
        var settings = Microsoft.Extensions.Options.Options.Create(new DetectionSettings
        {
            ModelPath           = modelPath,
            ConfidenceThreshold = threshold,
        });
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<SsdDetectionService>.Instance;
        return new SsdDetectionService(settings, logger);
    }

    public async Task<bool> IsPersonDetectedAsync(byte[] imageBytes)
    {
        var count = await CountPersonsAsync(imageBytes);
        return count > 0;
    }

    public async Task<int> CountPersonsAsync(byte[] imageBytes)
    {
        return await Task.Run(() =>
        {
            var (classes, scores, _) = RunInference(imageBytes);
            return CountPersons(classes, scores);
        });
    }

    public async Task<float> GetMaxPersonConfidenceAsync(byte[] imageBytes)
    {
        return await Task.Run(() =>
        {
            var (classes, scores, _) = RunInference(imageBytes);
            float max = 0f;
            for (int i = 0; i < 100; i++)
                if ((int)classes[i] == _settings.PersonClassId)
                    max = Math.Max(max, scores[i]);
            return max;
        });
    }

    public async Task<float> GetMaxPersonConfidenceInRoiAsync(
        byte[] imageBytes,
        IReadOnlyList<(float X, float Y)> roiPolygon)
    {
        return await Task.Run(() =>
        {
            var (classes, scores, boxes) = RunInference(imageBytes);
            float max = 0f;

            for (int i = 0; i < 100; i++)
            {
                if ((int)classes[i] != _settings.PersonClassId) continue;

                float ymin = boxes[i * 4 + 0];
                float xmin = boxes[i * 4 + 1];
                float ymax = boxes[i * 4 + 2];
                float xmax = boxes[i * 4 + 3];

                float cx = (xmin + xmax) / 2f;
                float cy = (ymin + ymax) / 2f;

                if (IsPointInPolygon(cx, cy, roiPolygon))
                    max = Math.Max(max, scores[i]);
            }

            return max;
        });
    }

    // Shared inference step: decode image, resize, build tensor, run ONNX, return raw outputs.
    // Returns classes, scores, and boxes arrays — all length 100 (one slot per candidate detection).
    private (float[] classes, float[] scores, float[] boxes) RunInference(byte[] imageBytes)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        using var image = Cv2.ImDecode(imageBytes, ImreadModes.Color);

        if (image.Empty())
        {
            _logger.LogWarning("ImDecode returned empty Mat — bytes may be corrupt");
            return (new float[100], new float[100], new float[400]);
        }

        var size = _settings.ModelInputSize;
        using var resized = new Mat();
        Cv2.Resize(image, resized, new OpenCvSharp.Size(size, size));

        var tensor = new DenseTensor<byte>(new[] { 1, size, size, 3 });
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var pixel          = resized.At<Vec3b>(y, x);
                tensor[0, y, x, 0] = pixel.Item2; // R
                tensor[0, y, x, 1] = pixel.Item1; // G
                tensor[0, y, x, 2] = pixel.Item0; // B
            }

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(_inputNodeName, tensor)
        };

        var swOnnx = System.Diagnostics.Stopwatch.StartNew();
        using var results    = _session.Run(inputs);
        swOnnx.Stop();

        var       resultList = results.ToList();

        float[] classes = resultList
            .First(r => r.Name == "detection_classes")
            .AsEnumerable<float>().ToArray();

        float[] scores = resultList
            .First(r => r.Name == "detection_scores")
            .AsEnumerable<float>().ToArray();

        float[] boxes = resultList
            .First(r => r.Name == "detection_boxes")
            .AsEnumerable<float>().ToArray();

        sw.Stop();
        _logger.LogInformation(
            "Inference timing — ONNX session: {OnnxMs} ms | total (decode+resize+tensor+ONNX): {TotalMs} ms",
            swOnnx.ElapsedMilliseconds, sw.ElapsedMilliseconds);

        return (classes, scores, boxes);
    }

    private static bool IsPointInPolygon(float px, float py, IReadOnlyList<(float X, float Y)> polygon)
    {
        bool inside = false;
        int  n      = polygon.Count;

        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            float xi = polygon[i].X, yi = polygon[i].Y;
            float xj = polygon[j].X, yj = polygon[j].Y;

            bool crossesRay = ((yi > py) != (yj > py))
                           && (px < (xj - xi) * (py - yi) / (yj - yi) + xi);

            if (crossesRay) inside = !inside;
        }

        return inside;
    }

    // Runs inference, draws bounding boxes and saves
    // Overwrites the previous file so only the latest result is kept per room.
    public async Task<string> DrawAndSaveAsync(byte[] imageBytes, string roomId)
    {
        return await Task.Run(() =>
        {
            var (classes, scores, boxes) = RunInference(imageBytes);

            using var image = Cv2.ImDecode(imageBytes, ImreadModes.Color);
            if (image.Empty()) return string.Empty;

            int W = image.Width;
            int H = image.Height;

            int personCount = 0;
            for (int i = 0; i < 100; i++)
            {
                if ((int)classes[i] != _settings.PersonClassId) continue;
                if (scores[i] < _settings.ConfidenceThreshold) continue;

                float ymin = boxes[i * 4 + 0];
                float xmin = boxes[i * 4 + 1];
                float ymax = boxes[i * 4 + 2];
                float xmax = boxes[i * 4 + 3];

                int rx = Math.Clamp((int)(xmin * W), 0, W - 1);
                int ry = Math.Clamp((int)(ymin * H), 0, H - 1);
                int rw = Math.Clamp((int)((xmax - xmin) * W), 1, W - rx);
                int rh = Math.Clamp((int)((ymax - ymin) * H), 1, H - ry);

                Cv2.Rectangle(image, new Rect(rx, ry, rw, rh), Scalar.Red, 2);
                Cv2.PutText(image, $"{scores[i]:P0}",
                    new Point(rx, Math.Max(ry - 6, 12)),
                    HersheyFonts.HersheySimplex, 0.5, Scalar.Red, 1);
                personCount++;
            }

            var labelText  = personCount > 0 ? $"OCCUPIED ({personCount} person(s))" : "EMPTY";
            var labelColor = personCount > 0 ? Scalar.Red : Scalar.Green;
            Cv2.PutText(image, labelText, new Point(10, 30),
                HersheyFonts.HersheySimplex, 0.9, labelColor, 2);

            // Use the hex camera ID from the URL as file name 
            var segments = roomId.TrimEnd('/').Split('/');
            var cameraLabel = segments.Length >= 2
                ? segments[^2]   // segment just before "snapshot"
                : segments[^1];

            Directory.CreateDirectory("Snapshots");
            var path = Path.Combine("Snapshots", $"{cameraLabel}_latest.jpg");
            Cv2.ImWrite(path, image);

            _logger.LogInformation(
                "Annotated snapshot saved → {Path} ({Label})", path, labelText);

            return path;
        });
    }

    private int CountPersons(float[] classes, float[] scores)
    {
        const int maxDetections = 100;
        int count = 0;

        for (int i = 0; i < maxDetections; i++)
        {
            int   classId    = (int)classes[i];
            float confidence = scores[i];

            if (classId == _settings.PersonClassId
                && confidence >= _settings.ConfidenceThreshold)
            {
                count++;
                _logger.LogDebug(
                    "Person {I}: classId={Class}, confidence={Conf:F2}",
                    i, classId, confidence);
            }
        }

        _logger.LogInformation(
            "Detection complete — {Count} person(s) found (threshold={Threshold})",
            count, _settings.ConfidenceThreshold);

        return count;
    }
}