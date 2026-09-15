namespace eBolnica.Application.Modules.Admin.Users.Queries.GetAdminProfile;

public sealed class GetAdminProfileQuery : IRequest<GetAdminProfileQueryDto>
{
}

public sealed class GetAdminProfileQueryDto
{
    public int AppUserId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string UserType { get; init; } = string.Empty;
    public bool IsAdmin { get; init; }
    public bool IsEnabled { get; init; }
}
