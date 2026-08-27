using AIImageAPI.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AIImageAPI.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<FaceEmbedding> FaceEmbeddings => Set<FaceEmbedding>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Customer>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Nickname).HasMaxLength(100);
            e.Property(x => x.Gender).HasMaxLength(20);
            e.Property(x => x.PhoneNumber).HasMaxLength(30);
            e.Property(x => x.Email).HasMaxLength(200);
            e.HasIndex(x => x.PhoneNumber);
        });

        b.Entity<FaceEmbedding>(e =>
        {
            e.Property(x => x.Model).HasMaxLength(50).IsRequired();
            e.Property(x => x.Vector).IsRequired();
            e.HasOne(x => x.Customer)
                .WithMany(c => c.Faces)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.CustomerId);
        });

        b.Entity<Product>(e =>
        {
            e.Property(x => x.Sku).HasMaxLength(64).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Category).HasMaxLength(100);
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.HasIndex(x => x.Sku).IsUnique();
        });

        b.Entity<Purchase>(e =>
        {
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.HasOne(x => x.Customer)
                .WithMany(c => c.Purchases)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CustomerId, x.PurchasedAt });
        });

        b.Entity<PurchaseItem>(e =>
        {
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.HasOne(x => x.Purchase)
                .WithMany(p => p.Items)
                .HasForeignKey(x => x.PurchaseId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.ProductId);
        });
    }
}
