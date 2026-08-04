namespace Market.Infrastructure.Database.Configurations.Clinical;

public sealed class MedicalRecordEntityConfiguration : IEntityTypeConfiguration<MedicalRecordEntity>
{
    public void Configure(EntityTypeBuilder<MedicalRecordEntity> b)
    {
        b.ToTable("MedicalRecords");
        b.HasKey(x => x.Id);
        b.Property(x => x.RecordNumber).IsRequired().HasMaxLength(50);
        b.HasIndex(x => x.RecordNumber).IsUnique();
        b.HasIndex(x => x.PatientId).IsUnique();
    }
}
