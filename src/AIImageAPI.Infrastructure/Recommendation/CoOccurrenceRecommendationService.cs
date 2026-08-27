using AIImageAPI.Core.Models;
using AIImageAPI.Core.Services;
using AIImageAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIImageAPI.Infrastructure.Recommendation;

/// <summary>
/// Recommends products by combining two signals:
///   1) Recency — products the customer bought most recently (re-purchase prompts).
///   2) Co-occurrence — products other customers frequently bought alongside this
///      customer's past products (item-based collaborative filtering).
///
/// Score = 0.6 * co-occurrence + 0.4 * recency, normalized.
/// </summary>
public class CoOccurrenceRecommendationService : IRecommendationService
{
    private readonly AppDbContext _db;

    public CoOccurrenceRecommendationService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<RecommendationResult>> GetRecommendationsAsync(
        Guid customerId,
        int max = 5,
        CancellationToken ct = default)
    {
        var pastProductIds = await _db.PurchaseItems
            .Where(pi => pi.Purchase!.CustomerId == customerId)
            .Select(pi => pi.ProductId)
            .Distinct()
            .ToListAsync(ct);

        if (pastProductIds.Count == 0)
            return Array.Empty<RecommendationResult>();

        var recentIds = await _db.PurchaseItems
            .Where(pi => pi.Purchase!.CustomerId == customerId)
            .OrderByDescending(pi => pi.Purchase!.PurchasedAt)
            .Select(pi => pi.ProductId)
            .Take(10)
            .ToListAsync(ct);

        var recentSet = recentIds.ToHashSet();

        var coOccurrence = await (
            from p1 in _db.PurchaseItems
            join p2 in _db.PurchaseItems on p1.PurchaseId equals p2.PurchaseId
            where pastProductIds.Contains(p1.ProductId)
                  && p2.ProductId != p1.ProductId
            group p2 by p2.ProductId into g
            select new { ProductId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(50)
            .ToListAsync(ct);

        if (coOccurrence.Count == 0 && recentSet.Count == 0)
            return Array.Empty<RecommendationResult>();

        var candidateIds = coOccurrence.Select(c => c.ProductId).Union(recentSet).ToList();
        var products = await _db.Products
            .Where(p => candidateIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var maxCoCount = coOccurrence.Count == 0 ? 1 : coOccurrence.Max(c => c.Count);

        var scored = new List<RecommendationResult>();
        foreach (var id in candidateIds)
        {
            if (!products.TryGetValue(id, out var product)) continue;
            var coCount = coOccurrence.FirstOrDefault(c => c.ProductId == id)?.Count ?? 0;
            var coScore = maxCoCount == 0 ? 0 : (double)coCount / maxCoCount;
            var recencyScore = recentSet.Contains(id) ? 1.0 : 0.0;
            var score = 0.6 * coScore + 0.4 * recencyScore;

            var reason = coCount > 0 && recencyScore > 0
                ? $"เคยซื้อ + ลูกค้าอื่นซื้อคู่กันบ่อย ({coCount} ครั้ง)"
                : coCount > 0
                    ? $"ลูกค้าอื่นมักซื้อคู่กัน ({coCount} ครั้ง)"
                    : "ลูกค้าเคยซื้อครั้งก่อน";

            scored.Add(new RecommendationResult(
                product.Id, product.Sku, product.Name, product.Price, reason, score));
        }

        return scored
            .OrderByDescending(r => r.Score)
            .Take(max)
            .ToList();
    }
}
