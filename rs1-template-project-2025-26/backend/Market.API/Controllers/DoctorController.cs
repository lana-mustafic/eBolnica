using Market.Application.Modules.Doctor.Patients.Queries.ListDoctorPatients;
using Market.Application.Modules.Doctor.Profile.Commands.UpdateDoctorProfile;
using Market.Application.Modules.Doctor.Profile.Queries.GetDoctorProfile;
using Market.Application.Modules.Doctor.Stats.Queries.GetDoctorStats;

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
}
