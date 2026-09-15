using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Infrastructure.Database.Configurations.Pharmacy;

public sealed class MedicationSupplierEntityConfiguration : IEntityTypeConfiguration<MedicationSupplierEntity>
{
    public void Configure(EntityTypeBuilder<MedicationSupplierEntity> b)
    {
        b.ToTable("MedicationSuppliers");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).IsRequired().HasMaxLength(150);
        b.Property(x => x.ContactPerson).HasMaxLength(100);
        b.Property(x => x.Email).HasMaxLength(150);
        b.Property(x => x.Phone).HasMaxLength(30);
        b.Property(x => x.Address).HasMaxLength(300);
        b.Property(x => x.TaxNumber).HasMaxLength(50);

        b.HasIndex(x => x.Name);
    }
}
