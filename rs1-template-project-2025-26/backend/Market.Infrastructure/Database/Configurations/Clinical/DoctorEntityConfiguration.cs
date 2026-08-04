namespace Market.Infrastructure.Database.Configurations.Clinical;

public sealed class DoctorEntityConfiguration : IEntityTypeConfiguration<DoctorEntity>
{
    public void Configure(EntityTypeBuilder<DoctorEntity> b)
    {
        b.ToTable("Doctors");
        b.HasKey(x => x.Id);

        b.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
        b.Property(x => x.LastName).IsRequired().HasMaxLength(100);
        b.Property(x => x.LicenseNumber).IsRequired().HasMaxLength(50);
        b.Property(x => x.RegistrationStatus).IsRequired().HasMaxLength(20);
        b.Property(x => x.Gender).HasMaxLength(20);
        b.Property(x => x.Specialization).HasMaxLength(100);
        b.Property(x => x.PhoneNumber).HasMaxLength(30);
        b.Property(x => x.Address).HasMaxLength(300);

        b.HasIndex(x => x.LicenseNumber).IsUnique();
        b.HasIndex(x => x.UserId).IsUnique();

        b.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
