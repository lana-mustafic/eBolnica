using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Infrastructure.Database.Configurations.Pharmacy;

public sealed class PrescriptionItemEntityConfiguration : IEntityTypeConfiguration<PrescriptionItemEntity>
{
    public void Configure(EntityTypeBuilder<PrescriptionItemEntity> b)
    {
        b.ToTable("PrescriptionItems");
        b.HasKey(x => x.Id);

        b.Property(x => x.Instructions).HasMaxLength(500);
        b.Property(x => x.UnitPrice).HasPrecision(18, 2);
        b.Property(x => x.TotalPrice).HasPrecision(18, 2);

        b.HasIndex(x => new { x.PrescriptionId, x.MedicationId });

        b.HasOne(x => x.Prescription)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Medication)
            .WithMany()
            .HasForeignKey(x => x.MedicationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
