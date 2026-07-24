using Microsoft.EntityFrameworkCore;
using OrderHub.Core.Domain;

namespace OrderHub.Infrastructure.Data;

public class OrderHubDbContext : DbContext
{
    public OrderHubDbContext(DbContextOptions<OrderHubDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(c => c.Name).HasMaxLength(100).IsRequired();
            entity.Property(c => c.Email).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Sku).HasMaxLength(20).IsRequired();
            entity.HasIndex(p => p.Sku).IsUnique();
            entity.Property(p => p.Name).HasMaxLength(100).IsRequired();
            entity.Property(p => p.UnitPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(o => o.DiscountRateSnapshot).HasPrecision(5, 4);
            entity.Property(o => o.TotalAmountSnapshot).HasPrecision(18, 2);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_Orders_DiscountRateSnapshot_Range",
                "[DiscountRateSnapshot] IS NULL OR ([DiscountRateSnapshot] >= 0 AND [DiscountRateSnapshot] <= 1)"));
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_Orders_PricingSnapshots_Complete",
                "([DiscountRateSnapshot] IS NULL AND [TotalAmountSnapshot] IS NULL) OR " +
                "([DiscountRateSnapshot] IS NOT NULL AND [TotalAmountSnapshot] IS NOT NULL)"));
            entity.HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(o => o.CreatedAt);
            entity.HasIndex(o => o.Status);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(i => i.UnitPriceSnapshot).HasPrecision(18, 2);
            entity.HasOne(i => i.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
