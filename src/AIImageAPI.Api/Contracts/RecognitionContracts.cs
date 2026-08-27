namespace AIImageAPI.Api.Contracts;

public record RecognizeRequest(string ImageBase64);

public record RecognizeResponse(
    bool Matched,
    float? Confidence,
    RecognizedCustomer? Customer,
    LastVisitInfo? LastVisit,
    IReadOnlyList<ProductRecommendation> Recommendations,
    DemographicEstimate Estimate,
    string Suggestion);

public record RecognizedCustomer(
    Guid Id,
    string FullName,
    string? Nickname,
    bool IsMember,
    DateTime? MemberSince);

public record DemographicEstimate(int Age, string Gender, float GenderConfidence);

public record LastVisitInfo(DateTime PurchasedAt, decimal TotalAmount, IReadOnlyList<PurchasedItem> Items);

public record PurchasedItem(Guid ProductId, string Name, int Quantity, decimal UnitPrice);

public record ProductRecommendation(Guid ProductId, string Sku, string Name, decimal Price, string Reason);
