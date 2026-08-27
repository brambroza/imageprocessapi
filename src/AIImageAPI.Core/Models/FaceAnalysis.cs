namespace AIImageAPI.Core.Models;

public record FaceBox(int X, int Y, int Width, int Height, float DetectionScore);

public record FaceAnalysis(
    FaceBox Box,
    float[] Embedding,
    int EstimatedAge,
    string EstimatedGender,
    float GenderConfidence);

public record FaceMatch(Guid CustomerId, float Similarity);
