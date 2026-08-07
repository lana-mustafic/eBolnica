using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Infrastructure.Database.Configurations.Pharmacy;

public sealed class PrescriptionNumberSequenceEntityConfiguration
    : IEntityTypeConfiguration<PrescriptionNumberSequenceEntity>
{
    public void Configure(EntityTypeBuilder<PrescriptionNumberSequenceEntity> b)
    {
        b.ToTable("PrescriptionNumberSequences");
        b.HasKey(x => x.Year);
        b.Property(x => x.Year).ValueGeneratedNever();
    }
}
