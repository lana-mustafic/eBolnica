using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Infrastructure.Database.Configurations.Pharmacy;

public sealed class StockReceiptEntityConfiguration : IEntityTypeConfiguration<StockReceiptEntity>
{
    public void Configure(EntityTypeBuilder<StockReceiptEntity> b)
    {
        b.ToTable("StockReceipts");
        b.HasKey(x => x.Id);

        b.Property(x => x.ReceiptNumber).IsRequired().HasMaxLength(32);
        b.Property(x => x.Notes).HasMaxLength(500);

        b.HasIndex(x => x.ReceiptNumber).IsUnique();

        b.HasOne(x => x.PurchaseOrder)
            .WithMany(x => x.Receipts)
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Pharmacist)
            .WithMany()
            .HasForeignKey(x => x.PharmacistId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
