using AIImageAPI.Core.Models;

namespace AIImageAPI.Core.Services;

public interface IRecommendationService
{
    Task<IReadOnlyList<RecommendationResult>> GetRecommendationsAsync(
        Guid customerId,
        int max = 5,
        CancellationToken ct = default);
}
