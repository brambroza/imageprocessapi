namespace AIImageAPI.Core.Entities;

public class FaceEmbedding
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public byte[] Vector { get; set; } = Array.Empty<byte>();
    public string Model { get; set; } = "buffalo_l";
    public int Dimension { get; set; } = 512;
    public DateTime CapturedAt { get; set; }
}
