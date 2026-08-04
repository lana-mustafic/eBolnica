namespace Market.Infrastructure.Database.Configurations.Clinical;

public sealed class PharmacistEntityConfiguration : IEntityTypeConfiguration<PharmacistEntity>
{
    public void Configure(EntityTypeBuilder<PharmacistEntity> b)
    {
        b.ToTable("Pharmacists");
        b.HasKey(x => x.Id);

        b.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
        b.Property(x => x.LastName).IsRequired().HasMaxLength(100);
        b.Property(x => x.LicenseNumber).IsRequired().HasMaxLength(50);
        b.Property(x => x.PhoneNumber).HasMaxLength(30);
        b.Property(x => x.Address).HasMaxLength(300);

        b.HasIndex(x => x.UserId).IsUnique();
        b.HasIndex(x => x.LicenseNumber).IsUnique();

        b.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
