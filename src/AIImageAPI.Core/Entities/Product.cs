namespace AIImageAPI.Core.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public decimal Price { get; set; }
}
