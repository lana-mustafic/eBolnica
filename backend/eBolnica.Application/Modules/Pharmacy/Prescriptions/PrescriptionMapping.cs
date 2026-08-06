using eBolnica.Application.Modules.Pharmacy.Prescriptions;
using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Application.Modules.Pharmacy.Prescriptions;

internal static class PrescriptionMapping
{
    public static PrescriptionDto MapToDto(PrescriptionEntity p) => new()
    {
        Id = p.Id,
        PrescriptionNumber = p.PrescriptionNumber,
        MedicalReportId = p.MedicalReportId,
        PatientId = p.PatientId,
        Patient = new PrescriptionPatientDto
        {
            Id = p.Patient.Id,
            FirstName = p.Patient.FirstName,
            LastName = p.Patient.LastName
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
            UnitPrice = i.UnitPrice
        }).ToList()
    };

    public static IQueryable<PrescriptionEntity> WithDetails(this IQueryable<PrescriptionEntity> query) =>
        query
            .Include(p => p.Patient)
            .Include(p => p.Doctor).ThenInclude(d => d.User)
            .Include(p => p.Pharmacist!).ThenInclude(ph => ph.User)
            .Include(p => p.Items).ThenInclude(i => i.Medication);
}
