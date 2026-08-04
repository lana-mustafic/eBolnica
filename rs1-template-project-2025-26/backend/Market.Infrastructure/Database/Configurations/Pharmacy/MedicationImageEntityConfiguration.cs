using Market.Domain.Entities.Pharmacy;

namespace Market.Infrastructure.Database.Configurations.Pharmacy;

public sealed class MedicationImageEntityConfiguration : IEntityTypeConfiguration<MedicationImageEntity>
{
    public void Configure(EntityTypeBuilder<MedicationImageEntity> b)
    {
        b.ToTable("MedicationImages");
        b.HasKey(x => x.Id);

        b.Property(x => x.FileName).IsRequired().HasMaxLength(255);
        b.Property(x => x.RelativeUrl).IsRequired().HasMaxLength(2048);

        b.HasIndex(x => x.MedicationId);

        b.HasOne(x => x.Medication)
            .WithMany(m => m.Images)
            .HasForeignKey(x => x.MedicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
