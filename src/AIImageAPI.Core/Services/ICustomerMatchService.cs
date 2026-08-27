using AIImageAPI.Core.Models;

namespace AIImageAPI.Core.Services;

public interface ICustomerMatchService
{
    /// <summary>
    /// Compare embedding against all cached customer embeddings.
    /// Returns the top match if similarity meets the configured threshold.
    /// </summary>
    FaceMatch? FindBestMatch(float[] embedding);

    /// <summary>
    /// Insert a new embedding into the in-memory cache. Call after persisting to DB.
    /// </summary>
    void AddToCache(Guid customerId, float[] embedding);

    /// <summary>
    /// Reload the cache from the underlying store (e.g. on startup or after bulk changes).
    /// </summary>
    Task ReloadAsync(CancellationToken ct = default);
}
