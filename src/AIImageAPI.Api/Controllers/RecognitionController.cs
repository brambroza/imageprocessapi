using AIImageAPI.Api.Contracts;
using AIImageAPI.Core.Services;
using AIImageAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIImageAPI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecognitionController : ControllerBase
{
    private readonly IFaceRecognitionService _face;
    private readonly ICustomerMatchService _match;
    private readonly IRecommendationService _recommend;
    private readonly AppDbContext _db;
    private readonly ILogger<RecognitionController> _log;

    public RecognitionController(
        IFaceRecognitionService face,
        ICustomerMatchService match,
        IRecommendationService recommend,
        AppDbContext db,
        ILogger<RecognitionController> log)
    {
        _face = face;
        _match = match;
        _recommend = recommend;
        _db = db;
        _log = log;
    }

    [HttpPost("recognize")]
    public async Task<ActionResult<RecognizeResponse>> Recognize(
        [FromBody] RecognizeRequest req,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.ImageBase64))
            return BadRequest(new { error = "imageBase64 is required" });

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(req.ImageBase64);
        }
        catch (FormatException)
        {
            return BadRequest(new { error = "imageBase64 is not valid base64" });
        }

        var analysis = await _face.AnalyzeAsync(bytes, ct);
        if (analysis is null)
        {
            return Ok(new RecognizeResponse(
                Matched: false,
                Confidence: null,
                Customer: null,
                LastVisit: null,
                Recommendations: Array.Empty<ProductRecommendation>(),
                Estimate: new DemographicEstimate(0, "unknown", 0f),
                Suggestion: "ตรวจไม่พบใบหน้าในภาพ กรุณาถ่ายใหม่"));
        }

        var estimate = new DemographicEstimate(
            analysis.EstimatedAge,
            analysis.EstimatedGender,
            analysis.GenderConfidence);

        var match = _match.FindBestMatch(analysis.Embedding);
        if (match is null)
        {
            return Ok(new RecognizeResponse(
                Matched: false,
                Confidence: null,
                Customer: null,
                LastVisit: null,
                Recommendations: Array.Empty<ProductRecommendation>(),
                Estimate: estimate,
                Suggestion: "ลูกค้าใหม่ — แนะให้พนักงานชวนสมัครสมาชิก"));
        }

        var customer = await _db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == match.CustomerId, ct);

        if (customer is null)
        {
            _log.LogWarning("Matched customer {Id} not found in DB", match.CustomerId);
            return Ok(new RecognizeResponse(false, null, null, null,
                Array.Empty<ProductRecommendation>(), estimate,
                "ข้อมูลลูกค้าไม่พบ — โปรดตรวจสอบระบบ"));
        }

        var lastPurchase = await _db.Purchases
            .AsNoTracking()
            .Where(p => p.CustomerId == customer.Id)
            .OrderByDescending(p => p.PurchasedAt)
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(ct);

        LastVisitInfo? lastVisit = null;
        if (lastPurchase is not null)
        {
            lastVisit = new LastVisitInfo(
                lastPurchase.PurchasedAt,
                lastPurchase.TotalAmount,
                lastPurchase.Items.Select(i => new PurchasedItem(
                    i.ProductId,
                    i.Product?.Name ?? "(unknown)",
                    i.Quantity,
                    i.UnitPrice)).ToList());
        }

        var recs = await _recommend.GetRecommendationsAsync(customer.Id, max: 5, ct);
        var recDtos = recs.Select(r =>
            new ProductRecommendation(r.ProductId, r.Sku, r.Name, r.Price, r.Reason)).ToList();

        return Ok(new RecognizeResponse(
            Matched: true,
            Confidence: match.Similarity,
            Customer: new RecognizedCustomer(
                customer.Id, customer.FullName, customer.Nickname,
                customer.IsMember, customer.MemberSince),
            LastVisit: lastVisit,
            Recommendations: recDtos,
            Estimate: estimate,
            Suggestion: customer.IsMember
                ? "ลูกค้าสมาชิก — แนะนำสินค้าจากประวัติซื้อ"
                : "ลูกค้าเคยมา แต่ยังไม่ได้เป็นสมาชิก — แนะให้สมัคร"));
    }
}
