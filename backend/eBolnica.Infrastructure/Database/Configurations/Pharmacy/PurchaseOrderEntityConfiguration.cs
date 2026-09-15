using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Infrastructure.Database.Configurations.Pharmacy;

public sealed class PurchaseOrderEntityConfiguration : IEntityTypeConfiguration<PurchaseOrderEntity>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderEntity> b)
    {
        b.ToTable("PurchaseOrders");
        b.HasKey(x => x.Id);

        b.Property(x => x.OrderNumber).IsRequired().HasMaxLength(32);
        b.Property(x => x.Status).IsRequired().HasMaxLength(32);
        b.Property(x => x.TotalAmount).HasPrecision(18, 2);
        b.Property(x => x.Notes).HasMaxLength(500);

        b.HasIndex(x => x.OrderNumber).IsUnique();
        b.HasIndex(x => new { x.Status, x.OrderedAtUtc });

        b.HasOne(x => x.Supplier)
            .WithMany(x => x.PurchaseOrders)
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Pharmacist)
            .WithMany()
            .HasForeignKey(x => x.PharmacistId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
