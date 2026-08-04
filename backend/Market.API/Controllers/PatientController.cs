using Market.Application.Modules.Patient.Profile.Queries.GetPatientProfile;



[ApiController]

[Route("api/patient")]

[Authorize(Policy = "PatientOnly")]

public sealed class PatientController(IMediator mediator) : ControllerBase

{

    [HttpGet("patient-data")]

    public async Task<ActionResult<GetPatientProfileQueryDto>> GetPatientData(CancellationToken ct)

        => Ok(await mediator.Send(new GetPatientProfileQuery(), ct));

}


