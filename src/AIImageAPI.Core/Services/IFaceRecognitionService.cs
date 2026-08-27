using AIImageAPI.Core.Models;

namespace AIImageAPI.Core.Services;

public interface IFaceRecognitionService
{
    /// <summary>
    /// Detect the largest face in the image and return its embedding + demographic estimation.
    /// Returns null when no face is detected.
    /// </summary>
    Task<FaceAnalysis?> AnalyzeAsync(byte[] imageBytes, CancellationToken ct = default);
}
