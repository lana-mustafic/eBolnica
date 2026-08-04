namespace Market.Infrastructure.Database.Configurations.Clinical;

public sealed class MedicalReportEntityConfiguration : IEntityTypeConfiguration<MedicalReportEntity>
{
    public void Configure(EntityTypeBuilder<MedicalReportEntity> b)
    {
        b.ToTable("MedicalReports");
        b.HasKey(x => x.Id);

        b.Property(x => x.Diagnosis).HasMaxLength(500);
        b.Property(x => x.Symptoms).HasMaxLength(500);
        b.Property(x => x.Therapy).HasMaxLength(500);
        b.Property(x => x.Description).HasMaxLength(2000);

        b.HasOne(x => x.MedicalRecord)
            .WithMany()
            .HasForeignKey(x => x.MedicalRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Doctor)
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
