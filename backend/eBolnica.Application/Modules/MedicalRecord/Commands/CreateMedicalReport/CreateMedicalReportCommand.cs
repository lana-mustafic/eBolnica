namespace eBolnica.Application.Modules.MedicalRecord.Commands.CreateMedicalReport;

public sealed class CreateMedicalReportCommand : IRequest<CreateMedicalReportCommandDto>
{
    public int MedicalRecordId { get; init; }
    public string? Symptoms { get; init; }
    public string? Diagnosis { get; init; }
    public string? Therapy { get; init; }
    public string? Description { get; init; }
}

public sealed class CreateMedicalReportCommandDto
{
    public int Id { get; init; }
}
