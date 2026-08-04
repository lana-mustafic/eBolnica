using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Infrastructure.Database.Configurations.Pharmacy;

public sealed class PrescriptionEntityConfiguration : IEntityTypeConfiguration<PrescriptionEntity>
{
    public void Configure(EntityTypeBuilder<PrescriptionEntity> b)
    {
        b.ToTable("Prescriptions");
        b.HasKey(x => x.Id);

        b.Property(x => x.PrescriptionNumber).IsRequired().HasMaxLength(32);
        b.Property(x => x.Status).IsRequired().HasMaxLength(32);
        b.Property(x => x.TotalAmount).HasPrecision(18, 2);
        b.Property(x => x.Notes).HasMaxLength(500);

        b.HasIndex(x => x.PrescriptionNumber).IsUnique();
        b.HasIndex(x => new { x.Status, x.CreatedAtUtc });
        b.HasIndex(x => new { x.PatientId, x.Status });

        b.HasOne(x => x.MedicalReport)
            .WithMany()
            .HasForeignKey(x => x.MedicalReportId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Doctor)
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Pharmacist)
            .WithMany()
            .HasForeignKey(x => x.PharmacistId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
