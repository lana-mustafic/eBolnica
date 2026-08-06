namespace eBolnica.Application.Modules.Pharmacy.Prescriptions.Queries.SearchPrescriptionPatients;

public sealed class SearchPrescriptionPatientsQuery : IRequest<IReadOnlyList<PrescriptionFormPatientDto>>
{
    public string? Search { get; set; }
    public int Limit { get; set; } = 20;
}

public sealed class PrescriptionFormPatientDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}
