using eBolnica.Application.Modules.Auth.Commands.Login;
using eBolnica.Application.Modules.Auth.Commands.RegisterDoctor;
using eBolnica.Application.Modules.Auth.Commands.RegisterPatient;

[ApiController]
[Route("api/accounts")]
public sealed class AccountsController(IMediator mediator) : ControllerBase
{
    [HttpPost("patient-registration")]
    [AllowAnonymous]
    public async Task<ActionResult<RegisterPatientCommandDto>> RegisterPatient(
        [FromBody] RegisterPatientCommand command,
        CancellationToken ct)
    {
        return Ok(await mediator.Send(command, ct));
    }

    [HttpPost("doctor-registration")]
    [AllowAnonymous]
    public async Task<ActionResult<RegisterDoctorCommandDto>> RegisterDoctor(
        [FromBody] RegisterDoctorCommand command,
        CancellationToken ct)
    {
        return Ok(await mediator.Send(command, ct));
    }

    /// <summary>
    /// eBolnica-compatible login endpoint (returns single JWT token).
    /// </summary>
    [HttpPost("user-login")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> UserLogin([FromBody] LoginCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Ok(new { Token = result.AccessToken });
    }
}
