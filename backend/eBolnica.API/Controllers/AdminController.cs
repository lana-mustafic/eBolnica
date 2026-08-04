using eBolnica.Application.Modules.Admin.Users.Commands.CreateUser;
using eBolnica.Application.Modules.Admin.Users.Commands.DeleteUser;
using eBolnica.Application.Modules.Admin.Users.Commands.UpdateDoctorRegistrationStatus;
using eBolnica.Application.Modules.Admin.Users.Commands.UpdatePatientRegistrationStatus;
using eBolnica.Application.Modules.Admin.Users.Commands.UpdateUser;
using eBolnica.Application.Modules.Admin.Users.Queries.List;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminController(IMediator mediator) : ControllerBase
{
    [HttpGet("list-users")]
    public async Task<ActionResult<ListUsersQueryDto>> ListUsers([FromQuery] ListUsersQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    [HttpPut("update-registration-status/{appUserId:int}")]
    public async Task<ActionResult<object>> UpdateDoctorRegistrationStatus(
        int appUserId,
        [FromBody] UpdateRegistrationStatusRequest body,
        CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateDoctorRegistrationStatusCommand
        {
            AppUserId = appUserId,
            RegistrationStatus = body.RegistrationStatus
        }, ct);

        return Ok(result);
    }

    [HttpPut("update-patient-registration-status/{appUserId:int}")]
    public async Task<ActionResult<object>> UpdatePatientRegistrationStatus(
        int appUserId,
        [FromBody] UpdateRegistrationStatusRequest body,
        CancellationToken ct)
    {
        var result = await mediator.Send(new UpdatePatientRegistrationStatusCommand
        {
            AppUserId = appUserId,
            RegistrationStatus = body.RegistrationStatus
        }, ct);

        return Ok(result);
    }

    [HttpPost("create-user")]
    public async Task<ActionResult<object>> CreateUser([FromBody] CreateUserCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPut("update-user/{appUserId:int}")]
    public async Task<ActionResult<object>> UpdateUser(
        int appUserId,
        [FromBody] UpdateUserBody body,
        CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateUserCommand
        {
            AppUserId = appUserId,
            FirstName = body.FirstName,
            LastName = body.LastName,
            Email = body.Email
        }, ct);

        return Ok(result);
    }

    [HttpDelete("delete-user/{appUserId:int}")]
    public async Task<ActionResult<object>> DeleteUser(int appUserId, CancellationToken ct)
        => Ok(await mediator.Send(new DeleteUserCommand { AppUserId = appUserId }, ct));
}

public sealed class UpdateRegistrationStatusRequest
{
    public string RegistrationStatus { get; set; } = string.Empty;
}

public sealed class UpdateUserBody
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
