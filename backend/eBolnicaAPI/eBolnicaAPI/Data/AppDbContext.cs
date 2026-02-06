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

            modelBuilder.Entity<MedicalRecord>()
                .HasMany(r => r.MedicalReports)
                .WithOne(rp => rp.MedicalRecord)
                .HasForeignKey(rp => rp.MedicalRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MedicalReport>()
                .HasIndex(r => r.MedicalRecordId);

            modelBuilder.Entity<Patient>()
                .HasMany(p=>p.Files)
                .WithOne(f=>f.Patient)
                .HasForeignKey(f=>f.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Pharmacist>()
                .HasOne(p => p.AppUser)
                .WithOne(u => u.Pharmacist)
                .HasForeignKey<Pharmacist>(p => p.AppUserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.MedicalReport)
                .WithMany()
                .HasForeignKey(p => p.MedicalReportId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.Patient)
                .WithMany()
                .HasForeignKey(p => p.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.Doctor)
                .WithMany()
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.Pharmacist)
                .WithMany(ph => ph.DispensedPrescriptions)
                .HasForeignKey(p => p.PharmacistId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Prescription>()
                .HasIndex(p => p.PrescriptionNumber)
                .IsUnique();

            // Performance optimization indexes for Prescriptions
            // Composite index for status and created date (common filter and sort combination)
            modelBuilder.Entity<Prescription>()
                .HasIndex(p => new { p.Status, p.CreatedAt })
                .HasDatabaseName("IX_Prescriptions_Status_CreatedAt");

            // Composite index for patient ID and status (common filter combination)
            modelBuilder.Entity<Prescription>()
                .HasIndex(p => new { p.PatientId, p.Status })
                .HasDatabaseName("IX_Prescriptions_PatientId_Status");

            // Index for prescribed date (common sort field)
            modelBuilder.Entity<Prescription>()
                .HasIndex(p => p.PrescribedDate)
                .HasDatabaseName("IX_Prescriptions_PrescribedDate");

            // Index for doctor ID (common filter)
            modelBuilder.Entity<Prescription>()
                .HasIndex(p => p.DoctorId)
                .HasDatabaseName("IX_Prescriptions_DoctorId");

            // Performance optimization indexes for Analytics queries
            // Composite index for dispensed date and status (revenue calculations)
            modelBuilder.Entity<Prescription>()
                .HasIndex(p => new { p.Status, p.DispensedDate })
                .HasDatabaseName("IX_Prescriptions_Status_DispensedDate");

            // Index for PrescriptionItems - MedicationId (category calculations)
            modelBuilder.Entity<PrescriptionItem>()
                .HasIndex(pi => pi.MedicationId)
                .HasDatabaseName("IX_PrescriptionItems_MedicationId");

            // Composite index for PrescriptionItems - PrescriptionId and MedicationId
            modelBuilder.Entity<PrescriptionItem>()
                .HasIndex(pi => new { pi.PrescriptionId, pi.MedicationId })
                .HasDatabaseName("IX_PrescriptionItems_PrescriptionId_MedicationId");

            // Index for Medications - Category (category queries)
            modelBuilder.Entity<Medication>()
                .HasIndex(m => new { m.Category, m.IsActive })
                .HasDatabaseName("IX_Medications_Category_IsActive");

            modelBuilder.Entity<PrescriptionItem>()
                .HasOne(pi => pi.Prescription)
                .WithMany(p => p.PrescriptionItems)
                .HasForeignKey(pi => pi.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PrescriptionItem>()
                .HasOne(pi => pi.Medication)
                .WithMany(m => m.PrescriptionItems)
                .HasForeignKey(pi => pi.MedicationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Medication>()
                .HasIndex(m => m.Name);

            // Performance optimization indexes for Medications
            // Composite index for name and category (common filter combination)
            modelBuilder.Entity<Medication>()
                .HasIndex(m => new { m.Name, m.Category })
                .HasDatabaseName("IX_Medications_Name_Category");

            // Composite index for price and stock quantity (common filter combination)
            modelBuilder.Entity<Medication>()
                .HasIndex(m => new { m.Price, m.StockQuantity })
                .HasDatabaseName("IX_Medications_Price_StockQuantity");

            // Composite index for active status and created date (default sorting)
            modelBuilder.Entity<Medication>()
                .HasIndex(m => new { m.IsActive, m.CreatedAt })
                .HasDatabaseName("IX_Medications_IsActive_CreatedAt");

            // Index for category (frequently filtered)
            modelBuilder.Entity<Medication>()
                .HasIndex(m => m.Category)
                .HasDatabaseName("IX_Medications_Category");

            // Index for expiry date (inventory alerts)
            modelBuilder.Entity<Medication>()
                .HasIndex(m => m.ExpiryDate)
                .HasDatabaseName("IX_Medications_ExpiryDate");

            // Index for stock quantity (low stock alerts)
            modelBuilder.Entity<Medication>()
                .HasIndex(m => m.StockQuantity)
                .HasDatabaseName("IX_Medications_StockQuantity");

            modelBuilder.Entity<Pharmacist>()
                .HasIndex(p => p.LicenseNumber)
                .IsUnique();

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

            var pharmacist1 = new AppUser
            {
                Id = "ph1",
                FirstName = "Milan",
                LastName = "Jovanović",
                UserName = "pharmacist@pharmacy.com",
                NormalizedUserName = "PHARMACIST@PHARMACY.COM",
                Email = "pharmacist@pharmacy.com",
                NormalizedEmail = "PHARMACIST@PHARMACY.COM",
                EmailConfirmed = false,
                PhoneNumber = "061200200",
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                UserType = "Pharmacist",
                LicenseNumber = "PH-L1",
                ConcurrencyStamp = "fixed-guid-1",
                SecurityStamp = "fixed-guid-2"
            };

            modelBuilder.Entity<AppUser>().HasData(admin, doctor1, doctor2, doctor3, patient1, patient2, patient3, patient4, patient5, patient6, patient7, patient8, patient9, patient10, patient11, patient12, pharmacist1);

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

            modelBuilder.Entity<Pharmacist>().HasData(
                new Pharmacist 
                { 
                    Id = 1, 
                    FirstName = pharmacist1.FirstName, 
                    LastName = pharmacist1.LastName, 
                    AppUserId = "ph1", 
                    LicenseNumber = pharmacist1.LicenseNumber, 
                    PhoneNumber = pharmacist1.PhoneNumber,
                    Address = "Apotekarska 15, Beograd",
                    HireDate = new DateTime(2020, 1, 15),
                    CreatedAt = new DateTime(2020, 1, 15)
                }
            );

            modelBuilder.Entity<Medication>().HasData(
                new Medication { Id = 1, Name = "Paracetamol", GenericName = "Acetaminophen", Description = "Pain reliever and fever reducer", Manufacturer = "PharmaCorp", Price = 250.00m, StockQuantity = 500, MinimumStockLevel = 100, ExpiryDate = new DateTime(2026, 12, 31), BatchNumber = "BATCH-001", IsActive = true, RequiresPrescription = false, Category = "Painkillers", DosageForm = "Tablet", Strength = "500mg", CreatedAt = new DateTime(2024, 1, 1) },
                new Medication { Id = 2, Name = "Ibuprofen", GenericName = "Ibuprofen", Description = "Nonsteroidal anti-inflammatory drug", Manufacturer = "MediPharm", Price = 320.00m, StockQuantity = 350, MinimumStockLevel = 80, ExpiryDate = new DateTime(2026, 6, 30), BatchNumber = "BATCH-002", IsActive = true, RequiresPrescription = false, Category = "Painkillers", DosageForm = "Tablet", Strength = "400mg", CreatedAt = new DateTime(2024, 1, 1) },
                new Medication { Id = 3, Name = "Amoxicillin", GenericName = "Amoxicillin", Description = "Antibiotic for bacterial infections", Manufacturer = "AntibioPharm", Price = 850.00m, StockQuantity = 200, MinimumStockLevel = 50, ExpiryDate = new DateTime(2025, 9, 15), BatchNumber = "BATCH-003", IsActive = true, RequiresPrescription = true, Category = "Antibiotics", DosageForm = "Capsule", Strength = "500mg", CreatedAt = new DateTime(2024, 1, 1) },
                new Medication { Id = 4, Name = "Aspirin", GenericName = "Acetylsalicylic acid", Description = "Pain reliever, anti-inflammatory, and blood thinner", Manufacturer = "PharmaCorp", Price = 180.00m, StockQuantity = 45, MinimumStockLevel = 50, ExpiryDate = new DateTime(2027, 3, 20), BatchNumber = "BATCH-004", IsActive = true, RequiresPrescription = false, Category = "Painkillers", DosageForm = "Tablet", Strength = "100mg", CreatedAt = new DateTime(2024, 1, 1) },
                new Medication { Id = 5, Name = "Cetirizine", GenericName = "Cetirizine", Description = "Antihistamine for allergies", Manufacturer = "AllergyMed", Price = 420.00m, StockQuantity = 280, MinimumStockLevel = 60, ExpiryDate = new DateTime(2026, 8, 10), BatchNumber = "BATCH-005", IsActive = true, RequiresPrescription = false, Category = "Antihistamines", DosageForm = "Tablet", Strength = "10mg", CreatedAt = new DateTime(2024, 1, 1) },
                new Medication { Id = 6, Name = "Omeprazole", GenericName = "Omeprazole", Description = "Proton pump inhibitor for acid reflux", Manufacturer = "DigestPharm", Price = 650.00m, StockQuantity = 150, MinimumStockLevel = 40, ExpiryDate = new DateTime(2025, 11, 30), BatchNumber = "BATCH-006", IsActive = true, RequiresPrescription = true, Category = "Gastrointestinal", DosageForm = "Capsule", Strength = "20mg", CreatedAt = new DateTime(2024, 1, 1) },
                new Medication { Id = 7, Name = "Metformin", GenericName = "Metformin", Description = "Antidiabetic medication", Manufacturer = "DiabetPharm", Price = 550.00m, StockQuantity = 120, MinimumStockLevel = 30, ExpiryDate = new DateTime(2026, 4, 15), BatchNumber = "BATCH-007", IsActive = true, RequiresPrescription = true, Category = "Antidiabetic", DosageForm = "Tablet", Strength = "500mg", CreatedAt = new DateTime(2024, 1, 1) },
                new Medication { Id = 8, Name = "Loratadine", GenericName = "Loratadine", Description = "Antihistamine for seasonal allergies", Manufacturer = "AllergyMed", Price = 380.00m, StockQuantity = 320, MinimumStockLevel = 70, ExpiryDate = new DateTime(2026, 7, 25), BatchNumber = "BATCH-008", IsActive = true, RequiresPrescription = false, Category = "Antihistamines", DosageForm = "Tablet", Strength = "10mg", CreatedAt = new DateTime(2024, 1, 1) },
                new Medication { Id = 9, Name = "Azithromycin", GenericName = "Azithromycin", Description = "Broad-spectrum antibiotic", Manufacturer = "AntibioPharm", Price = 1200.00m, StockQuantity = 80, MinimumStockLevel = 25, ExpiryDate = new DateTime(2025, 10, 5), BatchNumber = "BATCH-009", IsActive = true, RequiresPrescription = true, Category = "Antibiotics", DosageForm = "Tablet", Strength = "500mg", CreatedAt = new DateTime(2024, 1, 1) },
                new Medication { Id = 10, Name = "Vitamin D3", GenericName = "Cholecalciferol", Description = "Vitamin D supplement", Manufacturer = "VitaminsPlus", Price = 450.00m, StockQuantity = 600, MinimumStockLevel = 150, ExpiryDate = new DateTime(2027, 1, 10), BatchNumber = "BATCH-010", IsActive = true, RequiresPrescription = false, Category = "Vitamins", DosageForm = "Capsule", Strength = "2000 IU", CreatedAt = new DateTime(2024, 1, 1) },
                new Medication { Id = 11, Name = "Ciprofloxacin", GenericName = "Ciprofloxacin", Description = "Fluoroquinolone antibiotic", Manufacturer = "AntibioPharm", Price = 950.00m, StockQuantity = 35, MinimumStockLevel = 20, ExpiryDate = new DateTime(2025, 8, 20), BatchNumber = "BATCH-011", IsActive = true, RequiresPrescription = true, Category = "Antibiotics", DosageForm = "Tablet", Strength = "500mg", CreatedAt = new DateTime(2024, 1, 1) },
                new Medication { Id = 12, Name = "Diclofenac", GenericName = "Diclofenac", Description = "NSAID for pain and inflammation", Manufacturer = "MediPharm", Price = 520.00m, StockQuantity = 180, MinimumStockLevel = 45, ExpiryDate = new DateTime(2026, 5, 12), BatchNumber = "BATCH-012", IsActive = true, RequiresPrescription = true, Category = "Painkillers", DosageForm = "Tablet", Strength = "50mg", CreatedAt = new DateTime(2024, 1, 1) },
                new Medication { Id = 13, Name = "Fexofenadine", GenericName = "Fexofenadine", Description = "Antihistamine for allergies", Manufacturer = "AllergyMed", Price = 480.00m, StockQuantity = 250, MinimumStockLevel = 55, ExpiryDate = new DateTime(2026, 9, 18), BatchNumber = "BATCH-013", IsActive = true, RequiresPrescription = false, Category = "Antihistamines", DosageForm = "Tablet", Strength = "120mg", CreatedAt = new DateTime(2024, 1, 1) },
                new Medication { Id = 14, Name = "Calcium Carbonate", GenericName = "Calcium Carbonate", Description = "Calcium supplement and antacid", Manufacturer = "VitaminsPlus", Price = 280.00m, StockQuantity = 400, MinimumStockLevel = 100, ExpiryDate = new DateTime(2027, 2, 28), BatchNumber = "BATCH-014", IsActive = true, RequiresPrescription = false, Category = "Supplements", DosageForm = "Tablet", Strength = "500mg", CreatedAt = new DateTime(2024, 1, 1) },
                new Medication { Id = 15, Name = "Atorvastatin", GenericName = "Atorvastatin", Description = "Cholesterol-lowering medication", Manufacturer = "CardioPharm", Price = 1100.00m, StockQuantity = 95, MinimumStockLevel = 25, ExpiryDate = new DateTime(2026, 11, 8), BatchNumber = "BATCH-015", IsActive = true, RequiresPrescription = true, Category = "Cardiovascular", DosageForm = "Tablet", Strength = "20mg", CreatedAt = new DateTime(2024, 1, 1) }
            );


        }

    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<AppUser> AppUsers { get; set; }

    public DbSet<FileEntity> Files { get; set; }
    public DbSet<MedicalRecord> MedicalRecords { get; set; }

    public DbSet<MedicalReport> MedicalReports { get; set; }

    public DbSet<Pharmacist> Pharmacists { get; set; }
    public DbSet<Medication> Medications { get; set; }
    public DbSet<Prescription> Prescriptions { get; set; }
    public DbSet<PrescriptionItem> PrescriptionItems { get; set; }
    }
}

