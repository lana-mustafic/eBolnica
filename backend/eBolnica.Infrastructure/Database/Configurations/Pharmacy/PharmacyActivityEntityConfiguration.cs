using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Infrastructure.Database.Configurations.Pharmacy;

public sealed class PharmacyActivityEntityConfiguration : IEntityTypeConfiguration<PharmacyActivityEntity>
{
    public void Configure(EntityTypeBuilder<PharmacyActivityEntity> b)
    {
        b.ToTable("PharmacyActivities");
        b.HasKey(x => x.Id);

        b.Property(x => x.EventType).IsRequired().HasMaxLength(50);
        b.Property(x => x.Category).IsRequired().HasMaxLength(30);
        b.Property(x => x.Severity).IsRequired().HasMaxLength(20);
        b.Property(x => x.Message).IsRequired().HasMaxLength(500);

        b.HasIndex(x => x.CreatedAtUtc);
        b.HasIndex(x => x.Category);

        b.HasOne(x => x.Prescription)
            .WithMany()
            .HasForeignKey(x => x.PrescriptionId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasOne(x => x.Medication)
            .WithMany()
            .HasForeignKey(x => x.MedicationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
