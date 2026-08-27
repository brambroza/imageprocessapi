namespace AIImageAPI.Core.Entities;

public class Customer
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? Gender { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public bool IsMember { get; set; }
    public DateTime? MemberSince { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<FaceEmbedding> Faces { get; set; } = new List<FaceEmbedding>();
    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
}
