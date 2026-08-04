namespace eBolnica.Application.Modules.Pharmacy.Prescriptions;

public sealed class PrescriptionPatientDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public sealed class PrescriptionDoctorDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Specialization { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
}

public sealed class PrescriptionPharmacistDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateTime? HireDate { get; set; }
    public string? Email { get; set; }
}

public sealed class PrescriptionItemDto
{
    public int Id { get; set; }
    public int PrescriptionId { get; set; }
    public int MedicationId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Instructions { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => UnitPrice * Quantity;
}

public sealed class PrescriptionDto
{
    public int Id { get; set; }
    public string PrescriptionNumber { get; set; } = string.Empty;
    public int MedicalReportId { get; set; }
    public int PatientId { get; set; }
    public PrescriptionPatientDto Patient { get; set; } = null!;
    public int DoctorId { get; set; }
    public PrescriptionDoctorDto Doctor { get; set; } = null!;
    public int? PharmacistId { get; set; }
    public PrescriptionPharmacistDto? Pharmacist { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime PrescribedDate { get; set; }
    public DateTime? DispensedDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public IReadOnlyList<PrescriptionItemDto> PrescriptionItems { get; set; } = Array.Empty<PrescriptionItemDto>();
}
