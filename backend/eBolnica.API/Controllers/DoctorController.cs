using eBolnica.Application.Modules.Doctor.Patients.Queries.ListDoctorPatients;
using eBolnica.Application.Modules.Doctor.Profile.Commands.UpdateDoctorProfile;
using eBolnica.Application.Modules.Doctor.Profile.Queries.GetDoctorProfile;
using eBolnica.Application.Modules.Doctor.Stats.Queries.GetDoctorStats;
using eBolnica.Application.Modules.Pharmacy.Prescriptions;
using eBolnica.Application.Modules.Pharmacy.Prescriptions.Commands.CreatePrescription;

[ApiController]
[Route("api/doctor")]
[Authorize(Policy = "DoctorOnly")]
public sealed class DoctorController(IMediator mediator) : ControllerBase
{
    [HttpGet("doctor-data")]
    public async Task<ActionResult<GetDoctorProfileQueryDto>> GetDoctorData(CancellationToken ct)
        => Ok(await mediator.Send(new GetDoctorProfileQuery(), ct));

    [HttpPut("edit-doctor")]
    public async Task<ActionResult<UpdateDoctorProfileCommandDto>> EditDoctor(
        [FromBody] UpdateDoctorProfileCommand command,
        CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpGet("list-patients")]
    public async Task<ActionResult<ListDoctorPatientsQueryDto>> ListPatients(
        [FromQuery] ListDoctorPatientsQuery query,
        CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    [HttpGet("doctor-stats")]
    public async Task<ActionResult<GetDoctorStatsQueryDto>> GetDoctorStats(CancellationToken ct)
        => Ok(await mediator.Send(new GetDoctorStatsQuery(), ct));

    [HttpPost("prescriptions")]
    public async Task<ActionResult<PrescriptionDto>> CreatePrescription(
        [FromBody] CreatePrescriptionCommand command,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Created($"/api/pharmacy/prescriptions/{result.Id}", result);
    }
}
