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

            var admin = new AppUser
            {
                Id = "a1",
                FirstName = "Admin",
                LastName = "User",
                UserName = "admin@gmail.com",
                NormalizedUserName = "ADMIN@GMAIL.COM",
                Email = "admin@gmail.com",
                NormalizedEmail = "ADMIN@GMAIL.COM",
                EmailConfirmed = true,
                PhoneNumber = "061000000",
                PhoneNumberConfirmed = true,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                UserType = "Admin",
                LicenseNumber = null,
                ConcurrencyStamp = "fixed-guid-3",
                SecurityStamp = "fixed-guid-3"
            };


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

            var patient4 = new AppUser
            {
                Id = "p4",
                FirstName = "Ana",
                LastName = "Jovanović",
                UserName = "ana@gmail.com",
                NormalizedUserName = "ANA@GMAIL.COM",
                Email = "ana@gmail.com",
                NormalizedEmail = "ANA@GMAIL.COM",
                EmailConfirmed = false,
                PhoneNumber = "061100106",
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                UserType = "Patient",
                LicenseNumber = null,
                ConcurrencyStamp = "fixed-guid-1",
                SecurityStamp = "fixed-guid-2"
            };

            var patient5 = new AppUser
            {
                Id = "p5",
                FirstName = "Marko",
                LastName = "Nikolić",
                UserName = "marko.patient@gmail.com",
                NormalizedUserName = "MARKO.PATIENT@GMAIL.COM",
                Email = "marko.patient@gmail.com",
                NormalizedEmail = "MARKO.PATIENT@GMAIL.COM",
                EmailConfirmed = false,
                PhoneNumber = "061100107",
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                UserType = "Patient",
                LicenseNumber = null,
                ConcurrencyStamp = "fixed-guid-1",
                SecurityStamp = "fixed-guid-2"
            };

            var patient6 = new AppUser
            {
                Id = "p6",
                FirstName = "Sara",
                LastName = "Stojanović",
                UserName = "sara@gmail.com",
                NormalizedUserName = "SARA@GMAIL.COM",
                Email = "sara@gmail.com",
                NormalizedEmail = "SARA@GMAIL.COM",
                EmailConfirmed = false,
                PhoneNumber = "061100108",
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                UserType = "Patient",
                LicenseNumber = null,
                ConcurrencyStamp = "fixed-guid-1",
                SecurityStamp = "fixed-guid-2"
            };

            var patient7 = new AppUser
            {
                Id = "p7",
                FirstName = "Nikola",
                LastName = "Popović",
                UserName = "nikola@gmail.com",
                NormalizedUserName = "NIKOLA@GMAIL.COM",
                Email = "nikola@gmail.com",
                NormalizedEmail = "NIKOLA@GMAIL.COM",
                EmailConfirmed = false,
                PhoneNumber = "061100109",
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                UserType = "Patient",
                LicenseNumber = null,
                ConcurrencyStamp = "fixed-guid-1",
                SecurityStamp = "fixed-guid-2"
            };

            var patient8 = new AppUser
            {
                Id = "p8",
                FirstName = "Jovana",
                LastName = "Milošević",
                UserName = "jovana@gmail.com",
                NormalizedUserName = "JOVANA@GMAIL.COM",
                Email = "jovana@gmail.com",
                NormalizedEmail = "JOVANA@GMAIL.COM",
                EmailConfirmed = false,
                PhoneNumber = "061100110",
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                UserType = "Patient",
                LicenseNumber = null,
                ConcurrencyStamp = "fixed-guid-1",
                SecurityStamp = "fixed-guid-2"
            };

            var patient9 = new AppUser
            {
                Id = "p9",
                FirstName = "Stefan",
                LastName = "Đorđević",
                UserName = "stefan@gmail.com",
                NormalizedUserName = "STEFAN@GMAIL.COM",
                Email = "stefan@gmail.com",
                NormalizedEmail = "STEFAN@GMAIL.COM",
                EmailConfirmed = false,
                PhoneNumber = "061100111",
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                UserType = "Patient",
                LicenseNumber = null,
                ConcurrencyStamp = "fixed-guid-1",
                SecurityStamp = "fixed-guid-2"
            };

            var patient10 = new AppUser
            {
                Id = "p10",
                FirstName = "Milica",
                LastName = "Radić",
                UserName = "milica@gmail.com",
                NormalizedUserName = "MILICA@GMAIL.COM",
                Email = "milica@gmail.com",
                NormalizedEmail = "MILICA@GMAIL.COM",
                EmailConfirmed = false,
                PhoneNumber = "061100112",
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                UserType = "Patient",
                LicenseNumber = null,
                ConcurrencyStamp = "fixed-guid-1",
                SecurityStamp = "fixed-guid-2"
            };

            var patient11 = new AppUser
            {
                Id = "p11",
                FirstName = "Luka",
                LastName = "Stefanović",
                UserName = "luka@gmail.com",
                NormalizedUserName = "LUKA@GMAIL.COM",
                Email = "luka@gmail.com",
                NormalizedEmail = "LUKA@GMAIL.COM",
                EmailConfirmed = false,
                PhoneNumber = "061100113",
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                UserType = "Patient",
                LicenseNumber = null,
                ConcurrencyStamp = "fixed-guid-1",
                SecurityStamp = "fixed-guid-2"
            };

            var patient12 = new AppUser
            {
                Id = "p12",
                FirstName = "Teodora",
                LastName = "Lazić",
                UserName = "teodora@gmail.com",
                NormalizedUserName = "TEODORA@GMAIL.COM",
                Email = "teodora@gmail.com",
                NormalizedEmail = "TEODORA@GMAIL.COM",
                EmailConfirmed = false,
                PhoneNumber = "061100114",
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                UserType = "Patient",
                LicenseNumber = null,
                ConcurrencyStamp = "fixed-guid-1",
                SecurityStamp = "fixed-guid-2"
            };

            modelBuilder.Entity<AppUser>().HasData(admin, doctor1, doctor2, doctor3, patient1, patient2, patient3, patient4, patient5, patient6, patient7, patient8, patient9, patient10, patient11, patient12);

            modelBuilder.Entity<Doctor>().HasData(
                new Doctor { Id = 1,FirstName=doctor1.FirstName, LastName=doctor1.LastName, AppUserId = "d1", Specialization="Cardiology",RegistrationStatus="Approved", Address="Address1", BirthDate= new DateTime(1995, 3, 15), LicenseNumber=doctor1.LicenseNumber, PhoneNumber=doctor1.PhoneNumber },
                new Doctor { Id = 2, FirstName = doctor2.FirstName, LastName = doctor2.LastName, AppUserId = "d2", Specialization = "Neurology", RegistrationStatus = "Approved", Address = "Address2", BirthDate = new DateTime(1993, 3, 15), LicenseNumber =doctor2.LicenseNumber, PhoneNumber= doctor2.PhoneNumber },
                new Doctor { Id = 3, FirstName = doctor3.FirstName, LastName = doctor3.LastName, AppUserId = "d3", Specialization = "Psychiatry", RegistrationStatus = "Approved", Address = "Address3", BirthDate = new DateTime(1991, 3, 15), LicenseNumber = doctor3.LicenseNumber, PhoneNumber = doctor3.PhoneNumber }
                );

            modelBuilder.Entity<Patient>().HasData(
                new Patient { Id = 1, FirstName = patient1.FirstName, LastName = patient1.LastName, AppUserId = "p1", Gender="Male",Address = "Address1", DateOfBirth = new DateTime(1990, 3, 15), BloodType="A+", DoctorId=1, PhoneNumber=patient1.PhoneNumber },
                new Patient { Id = 2, FirstName =patient2.FirstName, LastName = patient2.LastName, AppUserId = "p2", Gender="Female",Address = "Address2", DateOfBirth = new DateTime(1991, 3, 15), BloodType = "B+", DoctorId = 1, PhoneNumber=patient2.PhoneNumber },
                new Patient { Id = 3, FirstName = patient3.FirstName, LastName =patient3.LastName, AppUserId = "p3", Gender="Other", Address = "Address3", DateOfBirth = new DateTime(1992, 3, 14), BloodType = "AB+", DoctorId = 1, PhoneNumber=patient3.PhoneNumber },
                new Patient { Id = 4, FirstName = patient4.FirstName, LastName = patient4.LastName, AppUserId = "p4", Gender="Female", Address = "Bulevar Kralja Aleksandra 15", DateOfBirth = new DateTime(1988, 5, 20), BloodType = "O+", DoctorId = 1, PhoneNumber=patient4.PhoneNumber },
                new Patient { Id = 5, FirstName = patient5.FirstName, LastName = patient5.LastName, AppUserId = "p5", Gender="Male", Address = "Knez Mihailova 25", DateOfBirth = new DateTime(1985, 7, 10), BloodType = "A-", DoctorId = 2, PhoneNumber=patient5.PhoneNumber },
                new Patient { Id = 6, FirstName = patient6.FirstName, LastName = patient6.LastName, AppUserId = "p6", Gender="Female", Address = "Nemanjina 10", DateOfBirth = new DateTime(1993, 9, 5), BloodType = "B-", DoctorId = 2, PhoneNumber=patient6.PhoneNumber },
                new Patient { Id = 7, FirstName = patient7.FirstName, LastName = patient7.LastName, AppUserId = "p7", Gender="Male", Address = "Terazije 5", DateOfBirth = new DateTime(1987, 11, 18), BloodType = "O-", DoctorId = 2, PhoneNumber=patient7.PhoneNumber },
                new Patient { Id = 8, FirstName = patient8.FirstName, LastName = patient8.LastName, AppUserId = "p8", Gender="Female", Address = "Vračar 20", DateOfBirth = new DateTime(1994, 12, 25), BloodType = "AB-", DoctorId = 3, PhoneNumber=patient8.PhoneNumber },
                new Patient { Id = 9, FirstName = patient9.FirstName, LastName = patient9.LastName, AppUserId = "p9", Gender="Male", Address = "Svetog Save 45", DateOfBirth = new DateTime(1989, 2, 12), BloodType = "A+", DoctorId = 1, PhoneNumber=patient9.PhoneNumber },
                new Patient { Id = 10, FirstName = patient10.FirstName, LastName = patient10.LastName, AppUserId = "p10", Gender="Female", Address = "Kralja Milana 30", DateOfBirth = new DateTime(1995, 6, 8), BloodType = "B+", DoctorId = 1, PhoneNumber=patient10.PhoneNumber },
                new Patient { Id = 11, FirstName = patient11.FirstName, LastName = patient11.LastName, AppUserId = "p11", Gender="Male", Address = "Obilićev venac 12", DateOfBirth = new DateTime(1986, 8, 22), BloodType = "O+", DoctorId = 1, PhoneNumber=patient11.PhoneNumber },
                new Patient { Id = 12, FirstName = patient12.FirstName, LastName = patient12.LastName, AppUserId = "p12", Gender="Female", Address = "Dunavska 8", DateOfBirth = new DateTime(1996, 4, 30), BloodType = "AB+", DoctorId = 1, PhoneNumber=patient12.PhoneNumber }
                );

            modelBuilder.Entity<MedicalRecord>().HasData(
                new MedicalRecord { Id=1, PatientId=1, RecordNumber="MRID1"},
                new MedicalRecord { Id = 2, PatientId = 2, RecordNumber = "MRID2" },
                new MedicalRecord { Id = 3, PatientId = 3, RecordNumber = "MRID3" },
                new MedicalRecord { Id = 4, PatientId = 4, RecordNumber = "MRID4" },
                new MedicalRecord { Id = 5, PatientId = 5, RecordNumber = "MRID5" },
                new MedicalRecord { Id = 6, PatientId = 6, RecordNumber = "MRID6" },
                new MedicalRecord { Id = 7, PatientId = 7, RecordNumber = "MRID7" },
                new MedicalRecord { Id = 8, PatientId = 8, RecordNumber = "MRID8" },
                new MedicalRecord { Id = 9, PatientId = 9, RecordNumber = "MRID9" },
                new MedicalRecord { Id = 10, PatientId = 10, RecordNumber = "MRID10" },
                new MedicalRecord { Id = 11, PatientId = 11, RecordNumber = "MRID11" },
                new MedicalRecord { Id = 12, PatientId = 12, RecordNumber = "MRID12" }
                );


        }

    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<AppUser> AppUsers { get; set; }

    public DbSet<MedicalRecord> MedicalRecords { get; set; }

    public DbSet<MedicalReport> MedicalReports { get; set; }
    }
}

