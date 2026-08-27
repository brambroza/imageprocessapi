using System.Collections.Concurrent;
using AIImageAPI.Core.Models;
using AIImageAPI.Core.Services;
using AIImageAPI.Infrastructure.Data;
using AIImageAPI.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIImageAPI.Infrastructure.Face;

/// <summary>
/// Keeps every customer's face embeddings in memory for fast cosine-similarity scanning.
/// Suitable up to ~100k embeddings per node. Swap for FAISS/pgvector/HNSW at higher scale.
/// </summary>
public sealed class InMemoryCustomerMatchService : ICustomerMatchService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FaceOptions _opts;
    private readonly ILogger<InMemoryCustomerMatchService> _log;
    private readonly ConcurrentBag<(Guid CustomerId, float[] Vector)> _cache = new();

    public InMemoryCustomerMatchService(
        IServiceScopeFactory scopeFactory,
        IOptions<FaceOptions> opts,
        ILogger<InMemoryCustomerMatchService> log)
    {
        _scopeFactory = scopeFactory;
        _opts = opts.Value;
        _log = log;
    }

    public FaceMatch? FindBestMatch(float[] embedding)
    {
        FaceMatch? best = null;
        foreach (var (customerId, vector) in _cache)
        {
            var sim = VectorMath.CosineSimilarity(embedding, vector);
            if (best is null || sim > best.Similarity)
                best = new FaceMatch(customerId, sim);
        }
        if (best is null || best.Similarity < _opts.CosineThreshold) return null;
        return best;
    }

    public void AddToCache(Guid customerId, float[] embedding)
    {
        _cache.Add((customerId, embedding));
    }

    public async Task ReloadAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rows = await db.FaceEmbeddings
            .AsNoTracking()
            .Select(f => new { f.CustomerId, f.Vector })
            .ToListAsync(ct);

        _cache.Clear();
        foreach (var row in rows)
            _cache.Add((row.CustomerId, VectorMath.UnpackFloats(row.Vector)));

        _log.LogInformation("Face embedding cache loaded: {Count} vectors", rows.Count);
    }
}
