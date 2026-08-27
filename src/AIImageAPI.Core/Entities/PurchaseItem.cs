namespace AIImageAPI.Core.Entities;

public class PurchaseItem
{
    public Guid Id { get; set; }
    public Guid PurchaseId { get; set; }
    public Purchase? Purchase { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
