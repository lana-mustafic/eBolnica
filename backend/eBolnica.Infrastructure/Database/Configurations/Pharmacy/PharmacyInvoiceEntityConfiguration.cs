using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Infrastructure.Database.Configurations.Pharmacy;

public sealed class PharmacyInvoiceEntityConfiguration : IEntityTypeConfiguration<PharmacyInvoiceEntity>
{
    public void Configure(EntityTypeBuilder<PharmacyInvoiceEntity> b)
    {
        b.ToTable("PharmacyInvoices");
        b.HasKey(x => x.Id);

        b.Property(x => x.InvoiceNumber).IsRequired().HasMaxLength(32);
        b.Property(x => x.Status).IsRequired().HasMaxLength(32);
        b.Property(x => x.TotalAmount).HasPrecision(18, 2);

        b.HasIndex(x => x.InvoiceNumber).IsUnique();
        b.HasIndex(x => x.PrescriptionId).IsUnique();

        b.HasOne(x => x.Prescription)
            .WithMany()
            .HasForeignKey(x => x.PrescriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Pharmacist)
            .WithMany()
            .HasForeignKey(x => x.PharmacistId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
