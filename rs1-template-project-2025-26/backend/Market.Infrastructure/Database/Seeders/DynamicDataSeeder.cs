using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Market.Domain.Entities.Identity;
using Market.Domain.Entities.Clinical;
using Market.Domain.Entities.Pharmacy;

namespace Market.Infrastructure.Database.Seeders;

public static class DynamicDataSeeder
{
    public static async Task SeedAsync(DatabaseContext context)
    {
        await context.Database.EnsureCreatedAsync();
        await SeedUsersAsync(context);
        await SeedPrescriptionsAsync(context);
    }

    private static async Task SeedUsersAsync(DatabaseContext context)
    {
        if (await context.Users.AnyAsync())
            return;

        var hasher = new PasswordHasher<MarketUserEntity>();

        var admin = new MarketUserEntity
        {
            Email = "admin@market.local",
            PasswordHash = hasher.HashPassword(null!, "Admin123!"),
            IsAdmin = true,
            UserType = UserTypes.Admin,
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var dummyForSwagger = new MarketUserEntity
        {
            Email = "string",
            PasswordHash = hasher.HashPassword(null!, "string"),
            UserType = UserTypes.Admin,
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var doctorUser = new MarketUserEntity
        {
            Email = "doctor@ebolnica.local",
            Firstname = "Ana",
            Lastname = "Kovač",
            PasswordHash = hasher.HashPassword(null!, "Doctor123!"),
            UserType = UserTypes.Doctor,
            LicenseNumber = "DOC-001",
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var patientUser = new MarketUserEntity
        {
            Email = "patient@ebolnica.local",
            Firstname = "Marko",
            Lastname = "Marković",
            PasswordHash = hasher.HashPassword(null!, "Patient123!"),
            UserType = UserTypes.Patient,
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Users.AddRange(admin, dummyForSwagger, doctorUser, patientUser);
        await context.SaveChangesAsync();

        var doctor = new DoctorEntity
        {
            UserId = doctorUser.Id,
            FirstName = doctorUser.Firstname,
            LastName = doctorUser.Lastname,
            LicenseNumber = "DOC-001",
            RegistrationStatus = "Approved",
            Specialization = "Cardiology",
            PhoneNumber = "+38761123456",
            Address = "Sarajevo, BiH",
            BirthDate = new DateTime(1985, 3, 15, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Doctors.Add(doctor);
        await context.SaveChangesAsync();

        var patient = new PatientEntity
        {
            UserId = patientUser.Id,
            DoctorId = doctor.Id,
            FirstName = patientUser.Firstname,
            LastName = patientUser.Lastname,
            RegistrationStatus = "Approved",
            DateOfBirth = new DateTime(1990, 7, 20, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            PhoneNumber = "+38762111222",
            Address = "Mostar, BiH",
            BloodType = "A+",
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var medicalRecord = new MedicalRecordEntity
        {
            PatientId = patient.Id,
            RecordNumber = $"MR-{DateTime.UtcNow:yyyy}-{patient.Id}",
            CreatedAtUtc = DateTime.UtcNow
        };

        context.MedicalRecords.Add(medicalRecord);
        await context.SaveChangesAsync();

        context.MedicalReports.AddRange(
            new MedicalReportEntity
            {
                MedicalRecordId = medicalRecord.Id,
                DoctorId = doctor.Id,
                Symptoms = "Chest pain, shortness of breath",
                Diagnosis = "Hypertension",
                Therapy = "Amlodipine 5mg daily",
                Description = "Initial consultation",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-30)
            },
            new MedicalReportEntity
            {
                MedicalRecordId = medicalRecord.Id,
                DoctorId = doctor.Id,
                Symptoms = "Follow-up, improved symptoms",
                Diagnosis = "Controlled hypertension",
                Therapy = "Continue current medication",
                Description = "Follow-up visit",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-7)
            });

        await context.SaveChangesAsync();

        await SeedPharmacistAndMedicationsAsync(context, hasher);

        // Pending patient for admin approval testing
        var pendingUser = new MarketUserEntity
        {
            Email = "pending.patient@ebolnica.local",
            Firstname = "Pending",
            Lastname = "Patient",
            PasswordHash = hasher.HashPassword(null!, "Patient123!"),
            UserType = UserTypes.Patient,
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        context.Users.Add(pendingUser);
        await context.SaveChangesAsync();

        var pendingPatient = new PatientEntity
        {
            UserId = pendingUser.Id,
            FirstName = pendingUser.Firstname,
            LastName = pendingUser.Lastname,
            RegistrationStatus = "Pending",
            CreatedAtUtc = DateTime.UtcNow
        };
        context.Patients.Add(pendingPatient);
        await context.SaveChangesAsync();

        context.MedicalRecords.Add(new MedicalRecordEntity
        {
            PatientId = pendingPatient.Id,
            RecordNumber = $"MR-{DateTime.UtcNow:yyyy}-{pendingPatient.Id}",
            CreatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        Console.WriteLine("✅ Dynamic seed: demo users added.");
    }

    private static async Task SeedPharmacistAndMedicationsAsync(DatabaseContext context, PasswordHasher<MarketUserEntity> hasher)
    {
        if (await context.Medications.AnyAsync())
            return;

        var pharmacistUser = new MarketUserEntity
        {
            Email = "pharmacist@ebolnica.local",
            Firstname = "Emir",
            Lastname = "Hadžić",
            PasswordHash = hasher.HashPassword(null!, "Pharmacist123!"),
            UserType = UserTypes.Pharmacist,
            LicenseNumber = "PH-001",
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Users.Add(pharmacistUser);
        await context.SaveChangesAsync();

        context.Pharmacists.Add(new PharmacistEntity
        {
            UserId = pharmacistUser.Id,
            FirstName = pharmacistUser.Firstname,
            LastName = pharmacistUser.Lastname,
            LicenseNumber = "PH-001",
            PhoneNumber = "+38763111222",
            Address = "Sarajevo, BiH",
            HireDate = DateTime.UtcNow.AddYears(-2),
            CreatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var expiry = DateTime.UtcNow.AddYears(2);
        context.Medications.AddRange(
            new MedicationEntity
            {
                Name = "Amlodipine 5mg",
                NormalizedName = MedicationEntity.NormalizeName("Amlodipine 5mg"),
                GenericName = "Amlodipine",
                Category = "Cardiovascular",
                DosageForm = "Tablet",
                Strength = "5mg",
                Manufacturer = "PharmaBiH",
                Price = 12.50m,
                StockQuantity = 120,
                MinimumStockLevel = 20,
                ExpiryDate = expiry,
                BatchNumber = "AML-2026-01",
                RequiresPrescription = true,
                IsActive = true,
                Description = "Calcium channel blocker for hypertension",
                CreatedAtUtc = DateTime.UtcNow
            },
            new MedicationEntity
            {
                Name = "Ibuprofen 400mg",
                NormalizedName = MedicationEntity.NormalizeName("Ibuprofen 400mg"),
                GenericName = "Ibuprofen",
                Category = "Analgesics",
                DosageForm = "Tablet",
                Strength = "400mg",
                Manufacturer = "Medica",
                Price = 5.99m,
                StockQuantity = 8,
                MinimumStockLevel = 15,
                ExpiryDate = expiry,
                BatchNumber = "IBU-2026-02",
                RequiresPrescription = false,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new MedicationEntity
            {
                Name = "Amoxicillin 500mg",
                NormalizedName = MedicationEntity.NormalizeName("Amoxicillin 500mg"),
                GenericName = "Amoxicillin",
                Category = "Antibiotics",
                DosageForm = "Capsule",
                Strength = "500mg",
                Manufacturer = "AntibioLab",
                Price = 18.00m,
                StockQuantity = 45,
                MinimumStockLevel = 10,
                ExpiryDate = expiry,
                BatchNumber = "AMX-2026-03",
                RequiresPrescription = true,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });

        await context.SaveChangesAsync();
    }

    private static async Task SeedPrescriptionsAsync(DatabaseContext context)
    {
        if (await context.Prescriptions.AnyAsync())
            return;

        var doctor = await context.Doctors.FirstOrDefaultAsync();
        var patient = await context.Patients.FirstOrDefaultAsync(p => p.DoctorId == doctor!.Id);
        var report = await context.MedicalReports.FirstOrDefaultAsync(r => r.DoctorId == doctor!.Id);
        var medications = await context.Medications.Where(m => m.IsActive).Take(2).ToListAsync();

        if (doctor is null || patient is null || report is null || medications.Count == 0)
            return;

        var now = DateTime.UtcNow;
        var amlodipine = medications.FirstOrDefault(m => m.Name.Contains("Amlodipine")) ?? medications[0];
        var ibuprofen = medications.FirstOrDefault(m => m.Name.Contains("Ibuprofen")) ?? medications[^1];

        var prescription = new PrescriptionEntity
        {
            PrescriptionNumber = $"RX-{now.Year}-0001",
            MedicalReportId = report.Id,
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            Status = PrescriptionStatuses.Pending,
            PrescribedDate = now.AddDays(-2),
            Notes = "Demo recept za test dispense flow-a",
            TotalAmount = amlodipine.Price * 2 + ibuprofen.Price * 1,
            CreatedAtUtc = now.AddDays(-2),
            Items =
            {
                new PrescriptionItemEntity
                {
                    MedicationId = amlodipine.Id,
                    Quantity = 2,
                    Instructions = "1 tableta ujutro",
                    UnitPrice = amlodipine.Price,
                    TotalPrice = amlodipine.Price * 2,
                    CreatedAtUtc = now.AddDays(-2)
                },
                new PrescriptionItemEntity
                {
                    MedicationId = ibuprofen.Id,
                    Quantity = 1,
                    Instructions = "Po potrebi",
                    UnitPrice = ibuprofen.Price,
                    TotalPrice = ibuprofen.Price,
                    CreatedAtUtc = now.AddDays(-2)
                }
            }
        };

        context.Prescriptions.Add(prescription);
        await context.SaveChangesAsync();
    }
}
