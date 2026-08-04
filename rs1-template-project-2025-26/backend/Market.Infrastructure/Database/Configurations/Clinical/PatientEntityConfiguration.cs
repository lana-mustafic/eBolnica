namespace Market.Infrastructure.Database.Configurations.Clinical;

public sealed class PatientEntityConfiguration : IEntityTypeConfiguration<PatientEntity>
{
    public void Configure(EntityTypeBuilder<PatientEntity> b)
    {
        b.ToTable("Patients");
        b.HasKey(x => x.Id);

        b.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
        b.Property(x => x.LastName).IsRequired().HasMaxLength(100);
        b.Property(x => x.RegistrationStatus).IsRequired().HasMaxLength(20);
        b.Property(x => x.Gender).HasMaxLength(20);
        b.Property(x => x.BloodType).HasMaxLength(10);
        b.Property(x => x.PhoneNumber).HasMaxLength(30);
        b.Property(x => x.Address).HasMaxLength(300);

        b.HasIndex(x => x.UserId).IsUnique();

        b.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Doctor)
            .WithMany(x => x.Patients)
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasOne(x => x.MedicalRecord)
            .WithOne(x => x.Patient)
            .HasForeignKey<MedicalRecordEntity>(x => x.PatientId);
    }
}
