using AIImageAPI.Core.Entities;
using AIImageAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIImageAPI.Api.Controllers;

public record CreatePurchaseRequest(
    Guid CustomerId,
    DateTime PurchasedAt,
    IReadOnlyList<CreatePurchaseLine> Lines);

public record CreatePurchaseLine(Guid ProductId, int Quantity, decimal UnitPrice);

[ApiController]
[Route("api/[controller]")]
public class PurchasesController : ControllerBase
{
    private readonly AppDbContext _db;
    public PurchasesController(AppDbContext db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePurchaseRequest req,
        CancellationToken ct)
    {
        var customer = await _db.Customers.FindAsync(new object[] { req.CustomerId }, ct);
        if (customer is null) return NotFound(new { error = "Customer not found" });

        var purchase = new Purchase
        {
            Id = Guid.NewGuid(),
            CustomerId = req.CustomerId,
            PurchasedAt = req.PurchasedAt,
            TotalAmount = req.Lines.Sum(l => l.UnitPrice * l.Quantity),
            Items = req.Lines.Select(l => new PurchaseItem
            {
                Id = Guid.NewGuid(),
                ProductId = l.ProductId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice
            }).ToList()
        };
        _db.Purchases.Add(purchase);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetForCustomer), new { customerId = req.CustomerId }, new { id = purchase.Id });
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<IActionResult> GetForCustomer(Guid customerId, CancellationToken ct)
    {
        var list = await _db.Purchases
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.PurchasedAt)
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .Take(20)
            .ToListAsync(ct);
        return Ok(list);
    }
}
