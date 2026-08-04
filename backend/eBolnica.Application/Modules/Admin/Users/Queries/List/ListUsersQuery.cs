namespace eBolnica.Application.Modules.Admin.Users.Queries.List;

public sealed class ListUsersQuery : IRequest<ListUsersQueryDto>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? UserType { get; init; }
    public string SortBy { get; init; } = "firstName";
    public string SortDirection { get; init; } = "asc";
}

public sealed class ListUsersQueryDto
{
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public string SortBy { get; init; } = string.Empty;
    public string SortDirection { get; init; } = string.Empty;
    public IReadOnlyList<UserOverviewDto> Users { get; init; } = Array.Empty<UserOverviewDto>();
}

public sealed class UserOverviewDto
{
    public int AppUserId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string UserType { get; init; } = string.Empty;
    public string? RegistrationStatus { get; init; }
    public string? LicenseNumber { get; init; }
    public int? DoctorProfileId { get; init; }
}
