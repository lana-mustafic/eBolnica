    using eBolnicaAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace eBolnicaAPI.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Patient>()
               .HasOne(p => p.AppUser)
               .WithOne(p=>p.Patient)
               .HasForeignKey<Patient>(p => p.AppUserId)
               .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.AppUser)
                .WithOne(u=>u.Doctor)
                .HasForeignKey<Doctor>(d => d.AppUserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Patient>()
                .HasOne(p => p.Doctor)
                .WithMany(d => d.Patients)
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.NoAction);

            var doctor1 = new AppUser
            {
                Id = "d1",
                FirstName = "Marko",
                LastName = "Marković",
                UserName = "marko@gmail.com",
                NormalizedUserName = "MARKO@GMAIL.COM",
                Email = "marko@gmail.com",
                NormalizedEmail = "MARKO@GMAIL.COM",
                EmailConfirmed = false,
                PhoneNumber = "061100100",
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                UserType = "Doctor",
                LicenseNumber = "L1",
                ConcurrencyStamp = "fixed-guid-1",
                SecurityStamp = "fixed-guid-2"
            };

            var doctor2 = new AppUser
            {
                Id = "d2",
                FirstName = "Senad",
                LastName = "Husić",
                UserName = "senad@gmail.com",
                NormalizedUserName = "SENAD@GMAIL.COM",
                Email = "senad@gmail.com",
                NormalizedEmail = "SENAD@GMAIL.COM",
                EmailConfirmed = false,
                PhoneNumber = "061100101",
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                UserType = "Doctor",
                LicenseNumber = "L2",
                ConcurrencyStamp = "fixed-guid-1",
                SecurityStamp = "fixed-guid-2"
            };

            var doctor3 = new AppUser
            {
                Id = "d3",
                FirstName = "Petar",
                LastName = "Petrović",
                UserName = "petar@gmail.com",
                NormalizedUserName = "PETAR@GMAIL.COM",
                Email = "petar@gmail.com",
                NormalizedEmail = "PETAR@GMAIL.COM",
                EmailConfirmed = false,
                PhoneNumber = "061100102",
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                UserType = "Doctor",
                LicenseNumber = "L3",
                ConcurrencyStamp = "fixed-guid-1",
                SecurityStamp = "fixed-guid-2"
            };

            var patient1 = new AppUser
            {
                Id = "p1",
                FirstName = "Ismet",
                LastName = "Horo",
                UserName = "ismet@gmail.com",
                NormalizedUserName = "ISMET@GMAIL.COM",
                Email = "ismet@gmail.com",
                NormalizedEmail = "ISMET@GMAIL.COM",
                EmailConfirmed = false,
                PhoneNumber = "061100103",
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                UserType = "Patient",
                LicenseNumber = null,
                ConcurrencyStamp = "fixed-guid-1",
                SecurityStamp = "fixed-guid-2"
            };

            var patient2 = new AppUser
            {
                Id = "p2",
                FirstName = "Elon",
                LastName = "Musk",
                UserName = "elon@gmail.com",
                NormalizedUserName = "ELON@GMAIL.COM",
                Email = "elon@gmail.com",
                NormalizedEmail = "ELON@GMAIL.COM",
                EmailConfirmed = false,
                PhoneNumber = "061100104",
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                UserType = "Patient",
                LicenseNumber = null,
                ConcurrencyStamp = "fixed-guid-1",
                SecurityStamp = "fixed-guid-2"
            };

            var patient3 = new AppUser
            {
                Id = "p3",
                FirstName = "Peter",
                LastName = "Griffin",
                UserName = "peter@gmail.com",
                NormalizedUserName = "PETER@GMAIL.COM",
                Email = "peter@gmail.com",
                NormalizedEmail = "PETER@GMAIL.COM",
                EmailConfirmed = false,
                PhoneNumber = "061100105",
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                UserType = "Patient",
                LicenseNumber = null,
                ConcurrencyStamp = "fixed-guid-1",
                SecurityStamp = "fixed-guid-2"
            };

            modelBuilder.Entity<AppUser>().HasData(doctor1, doctor2, doctor3, patient1, patient2, patient3);

            modelBuilder.Entity<Doctor>().HasData(
                new Doctor { Id = 1,FirstName=doctor1.FirstName, LastName=doctor1.LastName, AppUserId = "d1", Specialization="Cardiology",RegistrationStatus="Approved", Address="Address1", BirthDate= new DateTime(1995, 3, 15), LicenseNumber=doctor1.LicenseNumber, PhoneNumber=doctor1.PhoneNumber },
                new Doctor { Id = 2, FirstName = doctor2.FirstName, LastName = doctor2.LastName, AppUserId = "d2", Specialization = "Neurology", RegistrationStatus = "Approved", Address = "Address2", BirthDate = new DateTime(1993, 3, 15), LicenseNumber =doctor2.LicenseNumber, PhoneNumber= doctor2.PhoneNumber },
                new Doctor { Id = 3, FirstName = doctor3.FirstName, LastName = doctor3.LastName, AppUserId = "d3", Specialization = "Psychiatry", RegistrationStatus = "Approved", Address = "Address3", BirthDate = new DateTime(1991, 3, 15), LicenseNumber = doctor3.LicenseNumber, PhoneNumber = doctor3.PhoneNumber }
                );

            modelBuilder.Entity<Patient>().HasData(
                new Patient { Id = 1, FirstName = patient1.FirstName, LastName = patient1.LastName, AppUserId = "p1", Gender="Male",Address = "Address1", DateOfBirth = new DateTime(1990, 3, 15), MedicalRecordId="MRID1", BloodType="A", DoctorId=1, PhoneNumber=patient1.PhoneNumber },
                new Patient { Id = 2, FirstName =patient2.FirstName, LastName = patient2.LastName, AppUserId = "p2", Gender="Female",Address = "Address2", DateOfBirth = new DateTime(1991, 3, 15), MedicalRecordId = "MRID2", BloodType = "B", DoctorId = 1, PhoneNumber=patient2.PhoneNumber },
                new Patient { Id = 3, FirstName = patient3.FirstName, LastName =patient3.LastName, AppUserId = "p3", Gender="Male", Address = "Address3", DateOfBirth = new DateTime(1992, 3, 15), MedicalRecordId = "MRID3", BloodType = "0", DoctorId = 1, PhoneNumber=patient3.PhoneNumber }
                );

        }

    public DbSet<Doctor> Doctors { get; set; }

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
    public DbSet<Patient> Patients { get; set; }
    public DbSet<AppUser> AppUsers { get; set; }
    }
}


            modelBuilder.Entity<Prescription>(entity =>
            {
                entity.Property(p => p.Medication).HasMaxLength(100).IsRequired();
                entity.Property(p => p.Dosage).HasMaxLength(50).IsRequired();
            });
        }
    }
}