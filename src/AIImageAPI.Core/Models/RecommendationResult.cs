namespace AIImageAPI.Core.Models;

public record RecommendationResult(
    Guid ProductId,
    string Sku,
    string Name,
    decimal Price,
    string Reason,
    double Score);
