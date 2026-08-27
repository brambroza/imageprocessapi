using AIImageAPI.Core.Models;
using AIImageAPI.Core.Services;
using AIImageAPI.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace AIImageAPI.Infrastructure.Face;

/// <summary>
/// End-to-end face pipeline: detect largest face → align/crop 112x112 → embed → estimate age/gender.
///
/// This is a reference implementation intended to be wired to InsightFace ONNX exports
/// (SCRFD detector, ArcFace embedder, genderage predictor). Model tensor names, anchor
/// decoding and normalization vary by export — adjust the constants below to your model card.
/// </summary>
public sealed class OnnxFaceRecognitionService : IFaceRecognitionService, IDisposable
{
    private readonly FaceOptions _opts;
    private readonly ILogger<OnnxFaceRecognitionService> _log;
    private readonly InferenceSession _detector;
    private readonly InferenceSession _embedder;
    private readonly InferenceSession _ageGender;

    // ArcFace input mean/std (typical): images are (RGB - 127.5) / 128
    private const float ArcMean = 127.5f;
    private const float ArcStd = 128f;
    private const int EmbedSize = 112;

    public OnnxFaceRecognitionService(
        IOptions<FaceOptions> opts,
        ILogger<OnnxFaceRecognitionService> log)
    {
        _opts = opts.Value;
        _log = log;

        var dir = _opts.ModelsDirectory;
        var so = new SessionOptions { GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL };
        _detector = new InferenceSession(Path.Combine(dir, _opts.DetectorModel), so);
        _embedder = new InferenceSession(Path.Combine(dir, _opts.EmbedderModel), so);
        _ageGender = new InferenceSession(Path.Combine(dir, _opts.AgeGenderModel), so);
    }

    public async Task<FaceAnalysis?> AnalyzeAsync(byte[] imageBytes, CancellationToken ct = default)
    {
        using var image = Image.Load<Rgb24>(imageBytes);

        var box = await Task.Run(() => DetectLargestFace(image), ct);
        if (box is null)
        {
            _log.LogInformation("No face detected");
            return null;
        }

        using var crop = image.Clone(ctx => ctx
            .Crop(new Rectangle(box.X, box.Y, box.Width, box.Height))
            .Resize(EmbedSize, EmbedSize));

        var embedding = ExtractEmbedding(crop);
        var (age, gender, gConf) = PredictAgeGender(crop);

        return new FaceAnalysis(box, embedding, age, gender, gConf);
    }

    // -----------------------------------------------------------------
    // Detection (SCRFD) — placeholder. Anchor decoding depends on export.
    // A production impl should decode all three stride branches and NMS.
    // -----------------------------------------------------------------
    private FaceBox? DetectLargestFace(Image<Rgb24> image)
    {
        const int inputSize = 640;
        using var resized = image.Clone(c => c.Resize(new ResizeOptions
        {
            Size = new Size(inputSize, inputSize),
            Mode = ResizeMode.Pad
        }));

        var tensor = ImageToNchwTensor(resized, mean: 127.5f, std: 128f);
        var inputName = _detector.InputMetadata.Keys.First();
        using var outputs = _detector.Run(new[] { NamedOnnxValue.CreateFromTensor(inputName, tensor) });

        // NOTE: SCRFD post-processing (decode anchors per stride, apply NMS) is model-specific.
        // The stub below picks the maximum-score box from the first output for illustration.
        var scores = outputs.First().AsEnumerable<float>().ToArray();
        if (scores.Length == 0 || scores.Max() < 0.5f) return null;

        // TODO: replace with proper decoding. For now: full-image face box.
        return new FaceBox(0, 0, image.Width, image.Height, scores.Max());
    }

    private float[] ExtractEmbedding(Image<Rgb24> crop112)
    {
        var tensor = ImageToNchwTensor(crop112, ArcMean, ArcStd);
        var inputName = _embedder.InputMetadata.Keys.First();
        using var outputs = _embedder.Run(new[] { NamedOnnxValue.CreateFromTensor(inputName, tensor) });
        var raw = outputs.First().AsEnumerable<float>().ToArray();
        return L2Normalize(raw);
    }

    private (int age, string gender, float genderConfidence) PredictAgeGender(Image<Rgb24> crop112)
    {
        var tensor = ImageToNchwTensor(crop112, ArcMean, ArcStd);
        var inputName = _ageGender.InputMetadata.Keys.First();
        using var outputs = _ageGender.Run(new[] { NamedOnnxValue.CreateFromTensor(inputName, tensor) });
        var raw = outputs.First().AsEnumerable<float>().ToArray();

        // InsightFace genderage.onnx returns [female_score, male_score, age_normalized] typically.
        // Age is usually multiplied by 100 to obtain years.
        if (raw.Length < 3)
        {
            _log.LogWarning("Unexpected age/gender output length {Len}", raw.Length);
            return (0, "unknown", 0f);
        }
        var female = raw[0];
        var male = raw[1];
        var ageNorm = raw[2];
        var gender = male >= female ? "male" : "female";
        var conf = Math.Abs(male - female);
        var age = (int)Math.Round(ageNorm * 100f);
        return (age, gender, conf);
    }

    private static DenseTensor<float> ImageToNchwTensor(Image<Rgb24> img, float mean, float std)
    {
        var tensor = new DenseTensor<float>(new[] { 1, 3, img.Height, img.Width });
        img.ProcessPixelRows(rows =>
        {
            for (int y = 0; y < rows.Height; y++)
            {
                var row = rows.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    var px = row[x];
                    tensor[0, 0, y, x] = (px.R - mean) / std;
                    tensor[0, 1, y, x] = (px.G - mean) / std;
                    tensor[0, 2, y, x] = (px.B - mean) / std;
                }
            }
        });
        return tensor;
    }

    private static float[] L2Normalize(float[] v)
    {
        double sum = 0;
        for (int i = 0; i < v.Length; i++) sum += v[i] * v[i];
        var norm = Math.Sqrt(sum);
        if (norm < 1e-9) return v;
        var result = new float[v.Length];
        for (int i = 0; i < v.Length; i++) result[i] = (float)(v[i] / norm);
        return result;
    }

    public void Dispose()
    {
        _detector.Dispose();
        _embedder.Dispose();
        _ageGender.Dispose();
    }
}
