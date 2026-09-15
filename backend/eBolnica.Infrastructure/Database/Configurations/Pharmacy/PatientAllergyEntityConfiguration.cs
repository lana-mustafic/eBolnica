using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Infrastructure.Database.Configurations.Pharmacy;

public sealed class PatientAllergyEntityConfiguration : IEntityTypeConfiguration<PatientAllergyEntity>
{
    public void Configure(EntityTypeBuilder<PatientAllergyEntity> b)
    {
        b.ToTable("PatientAllergies");
        b.HasKey(x => x.Id);

        b.Property(x => x.Allergen).IsRequired().HasMaxLength(150);
        b.Property(x => x.Severity).IsRequired().HasMaxLength(32);
        b.Property(x => x.Reaction).HasMaxLength(300);
        b.Property(x => x.Notes).HasMaxLength(500);

        b.HasIndex(x => new { x.PatientId, x.Allergen });

        b.HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Medication)
            .WithMany()
            .HasForeignKey(x => x.MedicationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
