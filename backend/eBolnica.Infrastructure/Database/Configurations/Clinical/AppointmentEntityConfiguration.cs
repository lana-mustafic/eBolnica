using eBolnica.Domain.Entities.Clinical;

namespace eBolnica.Infrastructure.Database.Configurations.Clinical;

public sealed class AppointmentEntityConfiguration : IEntityTypeConfiguration<AppointmentEntity>
{
    public void Configure(EntityTypeBuilder<AppointmentEntity> b)
    {
        b.ToTable("Appointments");
        b.HasKey(x => x.Id);

        b.Property(x => x.Reason).IsRequired().HasMaxLength(300);
        b.Property(x => x.Status).IsRequired().HasMaxLength(32);
        b.Property(x => x.Notes).HasMaxLength(1000);

        b.HasIndex(x => new { x.DoctorId, x.ScheduledAtUtc });
        b.HasIndex(x => new { x.PatientId, x.ScheduledAtUtc });

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
