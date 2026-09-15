using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Infrastructure.Database.Configurations.Pharmacy;

public sealed class StockReceiptItemEntityConfiguration : IEntityTypeConfiguration<StockReceiptItemEntity>
{
    public void Configure(EntityTypeBuilder<StockReceiptItemEntity> b)
    {
        b.ToTable("StockReceiptItems");
        b.HasKey(x => x.Id);

        b.Property(x => x.BatchNumber).HasMaxLength(50);

        b.HasOne(x => x.StockReceipt)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.StockReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Medication)
            .WithMany()
            .HasForeignKey(x => x.MedicationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
