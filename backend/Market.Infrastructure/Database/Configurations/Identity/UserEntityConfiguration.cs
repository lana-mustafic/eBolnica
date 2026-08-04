namespace Market.Infrastructure.Database.Configurations.Identity;

public sealed class UserEntityConfiguration : IEntityTypeConfiguration<MarketUserEntity>
{
    public void Configure(EntityTypeBuilder<MarketUserEntity> b)
    {
        b.ToTable("Users");

        b.HasKey(x => x.Id);

        b.HasIndex(x => x.Email)
            .IsUnique();

        b.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(200);

        b.Property(x => x.PasswordHash)
            .IsRequired();

        // Roles
        b.Property(x => x.IsAdmin)
            .HasDefaultValue(false);

        b.Property(x => x.IsManager)
            .HasDefaultValue(false);

        b.Property(x => x.IsEmployee)
            .HasDefaultValue(true); // Default: regular user

        b.Property(x => x.TokenVersion)
            .HasDefaultValue(0);

        b.Property(x => x.IsEnabled)
            .HasDefaultValue(true);

        b.Property(x => x.UserType)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue(UserTypes.Patient);

        b.Property(x => x.LicenseNumber)
            .HasMaxLength(50);

        b.Property(x => x.Firstname)
            .HasMaxLength(100);

        b.Property(x => x.Lastname)
            .HasMaxLength(100);

        // Navigation
        b.HasMany(x => x.RefreshTokens)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId);
    }
}