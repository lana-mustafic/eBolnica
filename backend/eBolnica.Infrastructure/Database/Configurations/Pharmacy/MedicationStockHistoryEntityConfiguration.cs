using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Infrastructure.Database.Configurations.Pharmacy;

public sealed class MedicationStockHistoryEntityConfiguration : IEntityTypeConfiguration<MedicationStockHistoryEntity>
{
    public void Configure(EntityTypeBuilder<MedicationStockHistoryEntity> b)
    {
        b.ToTable("MedicationStockHistory");
        b.HasKey(x => x.Id);

        b.Property(x => x.Reason).IsRequired().HasMaxLength(50);

        b.HasIndex(x => x.MedicationId);
        b.HasIndex(x => x.CreatedAtUtc);

        b.HasOne(x => x.Medication)
            .WithMany()
            .HasForeignKey(x => x.MedicationId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Prescription)
            .WithMany()
            .HasForeignKey(x => x.PrescriptionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
