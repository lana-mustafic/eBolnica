using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using eBolnica.Domain.Entities.Identity;
using eBolnica.Domain.Entities.Clinical;
using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Infrastructure.Database.Seeders;

public static class DynamicDataSeeder
{
    public static async Task SeedAsync(DatabaseContext context)
    {
        await context.Database.EnsureCreatedAsync();
        await SeedUsersAsync(context);
        await SeedPrescriptionsAsync(context);
        await SeedClinicalAndProcurementAsync(context);
    }

    private static async Task SeedUsersAsync(DatabaseContext context)
    {
        if (await context.Users.AnyAsync())
            return;

        var hasher = new PasswordHasher<eBolnicaUserEntity>();

        var admin = new eBolnicaUserEntity
        {
            Email = "admin@ebolnica.local",
            PasswordHash = hasher.HashPassword(null!, "Admin123!"),
            IsAdmin = true,
            UserType = UserTypes.Admin,
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var dummyForSwagger = new eBolnicaUserEntity
        {
            Email = "string",
            PasswordHash = hasher.HashPassword(null!, "string"),
            UserType = UserTypes.Admin,
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var doctorUser = new eBolnicaUserEntity
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

        var patientUser = new eBolnicaUserEntity
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
        var pendingUser = new eBolnicaUserEntity
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

    private static async Task SeedPharmacistAndMedicationsAsync(DatabaseContext context, PasswordHasher<eBolnicaUserEntity> hasher)
    {
        if (await context.Medications.AnyAsync())
            return;

        var pharmacistUser = new eBolnicaUserEntity
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
                Category = "Kardiovaskularni",
                DosageForm = "Tableta",
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
                Category = "Analgetici",
                DosageForm = "Tableta",
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
                Category = "Antibiotici",
                DosageForm = "Kapsula",
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

        await SeedMedicationStockHistoryAsync(context);
    }

    private static async Task SeedMedicationStockHistoryAsync(DatabaseContext context)
    {
        if (await context.MedicationStockHistory.AnyAsync())
            return;

        var medications = await context.Medications.AsNoTracking().ToListAsync();
        foreach (var medication in medications)
        {
            context.MedicationStockHistory.Add(new MedicationStockHistoryEntity
            {
                MedicationId = medication.Id,
                ChangeQuantity = medication.StockQuantity,
                StockAfter = medication.StockQuantity,
                Reason = MedicationStockChangeReasons.InitialStock,
                CreatedAtUtc = medication.CreatedAtUtc
            });
        }

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

    private static async Task SeedClinicalAndProcurementAsync(DatabaseContext context)
    {
        var doctor = await context.Doctors.FirstOrDefaultAsync();
        var patient = await context.Patients.FirstOrDefaultAsync(p => p.RegistrationStatus == "Approved");
        var pharmacist = await context.Pharmacists.FirstOrDefaultAsync();
        var medications = await context.Medications.Where(m => m.IsActive).ToListAsync();
        var reports = await context.MedicalReports.OrderBy(r => r.CreatedAtUtc).ToListAsync();

        if (doctor is null || patient is null)
            return;

        if (!await context.Appointments.AnyAsync())
        {
            context.Appointments.AddRange(
                new AppointmentEntity
                {
                    PatientId = patient.Id,
                    DoctorId = doctor.Id,
                    ScheduledAtUtc = DateTime.UtcNow.AddDays(-7),
                    DurationMinutes = 30,
                    Reason = "Kontrolni pregled hipertenzije",
                    Status = AppointmentStatuses.Completed,
                    Notes = "Pacijent se osjeća bolje",
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-7)
                },
                new AppointmentEntity
                {
                    PatientId = patient.Id,
                    DoctorId = doctor.Id,
                    ScheduledAtUtc = DateTime.UtcNow.AddDays(14),
                    DurationMinutes = 20,
                    Reason = "Follow-up nakon terapije",
                    Status = AppointmentStatuses.Scheduled,
                    CreatedAtUtc = DateTime.UtcNow
                });
        }

        if (!await context.Hospitalizations.AnyAsync())
        {
            context.Hospitalizations.Add(new HospitalizationEntity
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                AdmittedAtUtc = DateTime.UtcNow.AddDays(-40),
                DischargedAtUtc = DateTime.UtcNow.AddDays(-35),
                Ward = "Kardiologija",
                RoomNumber = "214",
                AdmissionReason = "Povišen krvni pritisak, kratkoća daha",
                Status = HospitalizationStatuses.Discharged,
                Notes = "Otpušten uz ambulantno praćenje",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-40)
            });
        }

        if (!await context.ClinicalDiagnoses.AnyAsync() && reports.Count > 0)
        {
            foreach (var report in reports)
            {
                if (string.IsNullOrWhiteSpace(report.Diagnosis))
                    continue;

                context.ClinicalDiagnoses.Add(new ClinicalDiagnosisEntity
                {
                    PatientId = patient.Id,
                    DoctorId = report.DoctorId,
                    MedicalReportId = report.Id,
                    Code = report.Diagnosis.Contains("Hypertension", StringComparison.OrdinalIgnoreCase) ? "I10" : null,
                    Name = report.Diagnosis,
                    Description = report.Description,
                    DiagnosedAtUtc = report.CreatedAtUtc,
                    CreatedAtUtc = report.CreatedAtUtc
                });
            }
        }

        if (!await context.PatientAllergies.AnyAsync())
        {
            context.PatientAllergies.Add(new PatientAllergyEntity
            {
                PatientId = patient.Id,
                Allergen = "Penicillin",
                Severity = "High",
                Reaction = "Osip i otežano disanje",
                Notes = "Izbjegavati beta-laktamske antibiotike bez konsultacije",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        if (pharmacist is not null && medications.Count > 0 && !await context.MedicationSuppliers.AnyAsync())
        {
            var supplier = new MedicationSupplierEntity
            {
                Name = "PharmaDist d.o.o.",
                ContactPerson = "Lejla Smajić",
                Email = "nabavka@pharmadist.ba",
                Phone = "+38733222111",
                Address = "Sarajevo, BiH",
                TaxNumber = "4201234560007",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            context.MedicationSuppliers.Add(supplier);
            await context.SaveChangesAsync();

            var ibuprofen = medications.FirstOrDefault(m => m.Name.Contains("Ibuprofen")) ?? medications[0];
            var amoxicillin = medications.FirstOrDefault(m => m.Name.Contains("Amoxicillin")) ?? medications[^1];
            var now = DateTime.UtcNow;

            context.PurchaseOrders.Add(new PurchaseOrderEntity
            {
                OrderNumber = $"PO-{now.Year}-0001",
                SupplierId = supplier.Id,
                PharmacistId = pharmacist.Id,
                Status = PurchaseOrderStatuses.Ordered,
                OrderedAtUtc = now.AddDays(-1),
                ExpectedAtUtc = now.AddDays(3),
                Notes = "Dopuna niske zalihe Ibuprofena",
                TotalAmount = ibuprofen.Price * 50 + amoxicillin.Price * 20,
                CreatedAtUtc = now.AddDays(-1),
                Items =
                {
                    new PurchaseOrderItemEntity
                    {
                        MedicationId = ibuprofen.Id,
                        Quantity = 50,
                        UnitPrice = ibuprofen.Price,
                        CreatedAtUtc = now.AddDays(-1)
                    },
                    new PurchaseOrderItemEntity
                    {
                        MedicationId = amoxicillin.Id,
                        Quantity = 20,
                        UnitPrice = amoxicillin.Price,
                        CreatedAtUtc = now.AddDays(-1)
                    }
                }
            });
        }

        await context.SaveChangesAsync();
    }
}
