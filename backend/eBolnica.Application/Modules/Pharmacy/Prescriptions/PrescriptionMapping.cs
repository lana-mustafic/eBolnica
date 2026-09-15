using eBolnica.Application.Modules.Pharmacy.Prescriptions;
using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Application.Modules.Pharmacy.Prescriptions;

internal static class PrescriptionMapping
{
    public static PrescriptionDto MapToDto(PrescriptionEntity p) =>
        MapToDto(p, Array.Empty<PatientAllergyDto>(), null);

    public static PrescriptionDto MapToDto(
        PrescriptionEntity p,
        IReadOnlyList<PatientAllergyDto> allergies,
        PharmacyInvoiceDto? invoice) => new()
    {
        Id = p.Id,
        PrescriptionNumber = p.PrescriptionNumber,
        MedicalReportId = p.MedicalReportId,
        PatientId = p.PatientId,
        Patient = new PrescriptionPatientDto
        {
            Id = p.Patient.Id,
            FirstName = p.Patient.FirstName,
            LastName = p.Patient.LastName,
            PhoneNumber = p.Patient.PhoneNumber
        },
        DoctorId = p.DoctorId,
        Doctor = new PrescriptionDoctorDto
        {
            FirstName = p.Doctor.FirstName,
            LastName = p.Doctor.LastName,
            PhoneNumber = p.Doctor.PhoneNumber,
            Specialization = p.Doctor.Specialization,
            LicenseNumber = p.Doctor.LicenseNumber,
            BirthDate = p.Doctor.BirthDate,
            Address = p.Doctor.Address,
            Email = p.Doctor.User?.Email
        },
        PharmacistId = p.PharmacistId,
        Pharmacist = p.Pharmacist is null
            ? null
            : new PrescriptionPharmacistDto
            {
                Id = p.Pharmacist.Id,
                FirstName = p.Pharmacist.FirstName,
                LastName = p.Pharmacist.LastName,
                LicenseNumber = p.Pharmacist.LicenseNumber,
                PhoneNumber = p.Pharmacist.PhoneNumber,
                Address = p.Pharmacist.Address,
                HireDate = p.Pharmacist.HireDate,
                Email = p.Pharmacist.User?.Email
            },
        Status = p.Status,
        PrescribedDate = p.PrescribedDate,
        DispensedDate = p.DispensedDate,
        TotalAmount = p.TotalAmount,
        Notes = p.Notes,
        CreatedAt = p.CreatedAtUtc,
        UpdatedAt = p.ModifiedAtUtc,
        PrescriptionItems = p.Items.Select(i => new PrescriptionItemDto
        {
            Id = i.Id,
            PrescriptionId = i.PrescriptionId,
            MedicationId = i.MedicationId,
            MedicationName = i.Medication?.Name ?? "Obrisan lijek",
            Quantity = i.Quantity,
            Instructions = i.Instructions,
            UnitPrice = i.UnitPrice,
            StockQuantity = i.Medication?.StockQuantity,
            MinimumStockLevel = i.Medication?.MinimumStockLevel
        }).ToList(),
        Allergies = allergies,
        Invoice = invoice
    };

    public static IQueryable<PrescriptionEntity> WithDetails(this IQueryable<PrescriptionEntity> query) =>
        query
            .Include(p => p.Patient)
            .Include(p => p.Doctor).ThenInclude(d => d.User)
            .Include(p => p.Pharmacist!).ThenInclude(ph => ph.User)
            .Include(p => p.Items).ThenInclude(i => i.Medication);

    public static async Task<(IReadOnlyList<PatientAllergyDto> Allergies, PharmacyInvoiceDto? Invoice)> LoadRelatedAsync(
        IAppDbContext ctx,
        int patientId,
        int prescriptionId,
        CancellationToken ct)
    {
        var allergies = await ctx.PatientAllergies
            .AsNoTracking()
            .Include(a => a.Medication)
            .Where(a => a.PatientId == patientId)
            .Select(a => new PatientAllergyDto
            {
                Id = a.Id,
                Allergen = a.Allergen,
                Severity = a.Severity,
                Reaction = a.Reaction,
                MedicationId = a.MedicationId,
                MedicationName = a.Medication != null ? a.Medication.Name : null
            })
            .ToListAsync(ct);

        var invoice = await ctx.PharmacyInvoices
            .AsNoTracking()
            .Include(i => i.Items)
            .ThenInclude(item => item.Medication)
            .Where(i => i.PrescriptionId == prescriptionId)
            .Select(i => new PharmacyInvoiceDto
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                IssuedAtUtc = i.IssuedAtUtc,
                TotalAmount = i.TotalAmount,
                Status = i.Status,
                Items = i.Items.Select(item => new PharmacyInvoiceItemDto
                {
                    MedicationId = item.MedicationId,
                    MedicationName = item.Medication.Name,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        return (allergies, invoice);
    }
}
