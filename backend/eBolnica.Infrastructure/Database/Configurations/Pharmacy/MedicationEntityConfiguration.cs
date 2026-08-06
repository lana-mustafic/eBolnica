using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Infrastructure.Database.Configurations.Pharmacy;

public sealed class MedicationEntityConfiguration : IEntityTypeConfiguration<MedicationEntity>
{
    public void Configure(EntityTypeBuilder<MedicationEntity> b)
    {
        b.ToTable("Medications");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).IsRequired().HasMaxLength(100);
        b.Property(x => x.NormalizedName).IsRequired().HasMaxLength(100);
        b.Property(x => x.GenericName).HasMaxLength(100);
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.Manufacturer).HasMaxLength(100);
        b.Property(x => x.Price).HasPrecision(18, 2);
        b.Property(x => x.BatchNumber).HasMaxLength(50);
        b.Property(x => x.Category).HasMaxLength(50);
        b.Property(x => x.DosageForm).HasMaxLength(50);
        b.Property(x => x.Strength).HasMaxLength(50);
        b.Property(x => x.ImageUrl).HasMaxLength(2048);

        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => x.NormalizedName)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
