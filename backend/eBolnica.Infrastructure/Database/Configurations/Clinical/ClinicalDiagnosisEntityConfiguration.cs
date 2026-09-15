using eBolnica.Domain.Entities.Clinical;

namespace eBolnica.Infrastructure.Database.Configurations.Clinical;

public sealed class ClinicalDiagnosisEntityConfiguration : IEntityTypeConfiguration<ClinicalDiagnosisEntity>
{
    public void Configure(EntityTypeBuilder<ClinicalDiagnosisEntity> b)
    {
        b.ToTable("ClinicalDiagnoses");
        b.HasKey(x => x.Id);

        b.Property(x => x.Code).HasMaxLength(32);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(1000);

        b.HasIndex(x => new { x.PatientId, x.DiagnosedAtUtc });

        b.HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Doctor)
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.MedicalReport)
            .WithMany()
            .HasForeignKey(x => x.MedicalReportId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
