using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Infrastructure.Database.Configurations.Pharmacy;

public sealed class PurchaseOrderItemEntityConfiguration : IEntityTypeConfiguration<PurchaseOrderItemEntity>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItemEntity> b)
    {
        b.ToTable("PurchaseOrderItems");
        b.HasKey(x => x.Id);

        b.Property(x => x.UnitPrice).HasPrecision(18, 2);

        b.HasIndex(x => new { x.PurchaseOrderId, x.MedicationId });

        b.HasOne(x => x.PurchaseOrder)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Medication)
            .WithMany()
            .HasForeignKey(x => x.MedicationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
