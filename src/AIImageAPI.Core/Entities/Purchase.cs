namespace AIImageAPI.Core.Entities;

public class Purchase
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public DateTime PurchasedAt { get; set; }
    public decimal TotalAmount { get; set; }

    public ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
}
