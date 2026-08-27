namespace AIImageAPI.Api.Contracts;

public record CreateCustomerRequest(
    string FullName,
    string? Nickname,
    string? Gender,
    DateOnly? BirthDate,
    string? PhoneNumber,
    string? Email,
    bool IsMember,
    IReadOnlyList<string> FaceImagesBase64);

public record CustomerResponse(
    Guid Id,
    string FullName,
    string? Nickname,
    string? Gender,
    DateOnly? BirthDate,
    string? PhoneNumber,
    string? Email,
    bool IsMember,
    DateTime? MemberSince,
    int FaceCount);

public record AddFaceRequest(string ImageBase64);
