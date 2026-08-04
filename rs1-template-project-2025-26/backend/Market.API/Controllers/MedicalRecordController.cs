using Market.Application.Modules.MedicalRecord.Commands.CreateMedicalReport;
using Market.Application.Modules.MedicalRecord.Queries.GetMedicalRecordByPatientId;

[ApiController]
[Route("api/patient/medical-record")]
[Authorize(Policy = "DoctorOnly")]
public sealed class MedicalRecordController(IMediator mediator) : ControllerBase
{
    [HttpGet("{patientId:int}/medical-records")]
    public async Task<ActionResult<GetMedicalRecordByPatientIdQueryDto>> GetMedicalRecordByPatientId(
        int patientId,
        CancellationToken ct)
        => Ok(await mediator.Send(new GetMedicalRecordByPatientIdQuery { PatientId = patientId }, ct));

    [HttpPost("new-medical-report")]
    public async Task<ActionResult<CreateMedicalReportCommandDto>> CreateMedicalReport(
        [FromBody] CreateMedicalReportCommand command,
        CancellationToken ct)
        => Ok(await mediator.Send(command, ct));
}
