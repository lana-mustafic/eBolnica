using eBolnica.Domain.Entities.Clinical;

namespace eBolnica.Infrastructure.Database.Configurations.Clinical;

public sealed class HospitalizationEntityConfiguration : IEntityTypeConfiguration<HospitalizationEntity>
{
    public void Configure(EntityTypeBuilder<HospitalizationEntity> b)
    {
        b.ToTable("Hospitalizations");
        b.HasKey(x => x.Id);

        b.Property(x => x.Ward).IsRequired().HasMaxLength(100);
        b.Property(x => x.RoomNumber).HasMaxLength(20);
        b.Property(x => x.AdmissionReason).IsRequired().HasMaxLength(500);
        b.Property(x => x.Status).IsRequired().HasMaxLength(32);
        b.Property(x => x.Notes).HasMaxLength(1000);

        b.HasIndex(x => new { x.PatientId, x.Status });

        b.HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Doctor)
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
