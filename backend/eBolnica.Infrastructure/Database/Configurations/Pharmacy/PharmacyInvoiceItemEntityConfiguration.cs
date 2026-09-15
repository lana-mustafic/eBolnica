using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Infrastructure.Database.Configurations.Pharmacy;

public sealed class PharmacyInvoiceItemEntityConfiguration : IEntityTypeConfiguration<PharmacyInvoiceItemEntity>
{
    public void Configure(EntityTypeBuilder<PharmacyInvoiceItemEntity> b)
    {
        b.ToTable("PharmacyInvoiceItems");
        b.HasKey(x => x.Id);

        b.Property(x => x.UnitPrice).HasPrecision(18, 2);
        b.Property(x => x.TotalPrice).HasPrecision(18, 2);

        b.HasOne(x => x.Invoice)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Medication)
            .WithMany()
            .HasForeignKey(x => x.MedicationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
