using AIImageAPI.Api.Contracts;
using AIImageAPI.Core.Entities;
using AIImageAPI.Core.Services;
using AIImageAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIImageAPI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IFaceRecognitionService _face;
    private readonly ICustomerMatchService _match;
    private readonly ILogger<CustomersController> _log;

    public CustomersController(
        AppDbContext db,
        IFaceRecognitionService face,
        ICustomerMatchService match,
        ILogger<CustomersController> log)
    {
        _db = db;
        _face = face;
        _match = match;
        _log = log;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerResponse>> Get(Guid id, CancellationToken ct)
    {
        var c = await _db.Customers
            .AsNoTracking()
            .Include(x => x.Faces)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return NotFound();
        return Ok(new CustomerResponse(
            c.Id, c.FullName, c.Nickname, c.Gender, c.BirthDate,
            c.PhoneNumber, c.Email, c.IsMember, c.MemberSince, c.Faces.Count));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create(
        [FromBody] CreateCustomerRequest req,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.FullName))
            return BadRequest(new { error = "fullName is required" });

        var now = DateTime.UtcNow;
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = req.FullName,
            Nickname = req.Nickname,
            Gender = req.Gender,
            BirthDate = req.BirthDate,
            PhoneNumber = req.PhoneNumber,
            Email = req.Email,
            IsMember = req.IsMember,
            MemberSince = req.IsMember ? now : null,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Customers.Add(customer);

        var embeddingsAdded = 0;
        foreach (var b64 in req.FaceImagesBase64 ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(b64)) continue;
            var bytes = Convert.FromBase64String(b64);
            var analysis = await _face.AnalyzeAsync(bytes, ct);
            if (analysis is null)
            {
                _log.LogWarning("Face not detected in one of the provided images");
                continue;
            }
            var embedding = new FaceEmbedding
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                Vector = VectorMath.PackFloats(analysis.Embedding),
                Dimension = analysis.Embedding.Length,
                CapturedAt = now
            };
            _db.FaceEmbeddings.Add(embedding);
            _match.AddToCache(customer.Id, analysis.Embedding);
            embeddingsAdded++;
        }

        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = customer.Id },
            new CustomerResponse(
                customer.Id, customer.FullName, customer.Nickname, customer.Gender,
                customer.BirthDate, customer.PhoneNumber, customer.Email,
                customer.IsMember, customer.MemberSince, embeddingsAdded));
    }

    [HttpPost("{id:guid}/faces")]
    public async Task<IActionResult> AddFace(
        Guid id,
        [FromBody] AddFaceRequest req,
        CancellationToken ct)
    {
        var customer = await _db.Customers.FindAsync(new object[] { id }, ct);
        if (customer is null) return NotFound();

        var bytes = Convert.FromBase64String(req.ImageBase64);
        var analysis = await _face.AnalyzeAsync(bytes, ct);
        if (analysis is null) return BadRequest(new { error = "No face detected" });

        var embedding = new FaceEmbedding
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Vector = VectorMath.PackFloats(analysis.Embedding),
            Dimension = analysis.Embedding.Length,
            CapturedAt = DateTime.UtcNow
        };
        _db.FaceEmbeddings.Add(embedding);
        _match.AddToCache(customer.Id, analysis.Embedding);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}
