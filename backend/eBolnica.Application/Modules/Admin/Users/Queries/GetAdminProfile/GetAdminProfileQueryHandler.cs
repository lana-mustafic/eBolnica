using eBolnica.Application.Modules.Admin.Users.Queries.GetAdminProfile;

public sealed class GetAdminProfileQueryHandler(IAppDbContext ctx, IAppCurrentUser currentUser)
    : IRequestHandler<GetAdminProfileQuery, GetAdminProfileQueryDto>
{
    public async Task<GetAdminProfileQueryDto> Handle(GetAdminProfileQuery request, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            throw new eBolnicaBusinessRuleException("auth.not_authenticated", "User is not authenticated.");

        var user = await ctx.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUser.UserId.Value, ct)
            ?? throw new eBolnicaNotFoundException("User not found.");

        return new GetAdminProfileQueryDto
        {
            AppUserId = user.Id,
            FirstName = user.Firstname,
            LastName = user.Lastname,
            Email = user.Email,
            UserType = user.UserType,
            IsAdmin = user.IsAdmin,
            IsEnabled = user.IsEnabled
        };
    }
}
