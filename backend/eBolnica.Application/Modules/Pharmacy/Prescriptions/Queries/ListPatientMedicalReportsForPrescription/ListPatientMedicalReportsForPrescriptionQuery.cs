namespace eBolnica.Application.Modules.Pharmacy.Prescriptions.Queries.ListPatientMedicalReportsForPrescription;

public sealed class ListPatientMedicalReportsForPrescriptionQuery : IRequest<IReadOnlyList<PrescriptionFormMedicalReportDto>>
{
    public int PatientId { get; set; }
}

public sealed class PrescriptionFormMedicalReportDto
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Diagnosis { get; set; }
    public string DoctorFirstName { get; set; } = string.Empty;
    public string DoctorLastName { get; set; } = string.Empty;
    public string? DoctorSpecialization { get; set; }
}
