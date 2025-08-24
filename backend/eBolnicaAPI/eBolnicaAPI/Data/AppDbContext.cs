using eBolnicaAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace eBolnicaAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSet properties for all entities
        public DbSet<Doctor> Doctors { get; set; } = null!;
        public DbSet<Patient> Patients { get; set; } = null!;
        public DbSet<Appointment> Appointments { get; set; } = null!;
        public DbSet<MedicalRecord> MedicalRecords { get; set; } = null!;
        public DbSet<Prescription> Prescriptions { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Department-Doctor relationship (One Department -> Many Doctors)
            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.Department)
                .WithMany(dept => dept.Doctors)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Doctor-Appointment relationship (One Doctor -> Many Appointments)
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Patient-Appointment relationship (One Patient -> Many Appointments)
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Patient-MedicalRecord relationship (One Patient -> Many MedicalRecords)
            modelBuilder.Entity<MedicalRecord>()
                .HasOne(m => m.Patient)
                .WithMany(p => p.MedicalRecords)
                .HasForeignKey(m => m.PatientId)
                .OnDelete(DeleteBehavior.Cascade); // Delete records when patient is deleted

            // Configure Doctor-Prescription relationship (One Doctor -> Many Prescriptions)
            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.Doctor)
                .WithMany(d => d.Prescriptions)
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Patient-Prescription relationship (One Patient -> Many Prescriptions)
            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.Patient)
                .WithMany(pat => pat.Prescriptions)
                .HasForeignKey(p => p.PatientId)
                .OnDelete(DeleteBehavior.Cascade); // Delete prescriptions when patient is deleted

            // Optional: Configure unique constraints
            modelBuilder.Entity<Department>()
                .HasIndex(d => d.Name)
                .IsUnique();

            modelBuilder.Entity<Doctor>()
                .HasIndex(d => d.Email)
                .IsUnique();

            modelBuilder.Entity<Patient>()
                .HasIndex(p => p.Email)
                .IsUnique();

            // Configure string lengths and required fields
            modelBuilder.Entity<Department>(entity =>
            {
                entity.Property(d => d.Name).HasMaxLength(100).IsRequired();
                entity.Property(d => d.Location).HasMaxLength(50).IsRequired();
                entity.Property(d => d.PhoneNumber).HasMaxLength(20);
                entity.Property(d => d.Email).HasMaxLength(100);
            });

            modelBuilder.Entity<Prescription>(entity =>
            {
                entity.Property(p => p.Medication).HasMaxLength(100).IsRequired();
                entity.Property(p => p.Dosage).HasMaxLength(50).IsRequired();
            });
        }
    }
}