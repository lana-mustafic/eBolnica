using Market.Application.Modules.Doctor.Profile.Queries.GetDoctorProfile;

public sealed class GetDoctorProfileQueryHandler(IAppDbContext ctx, IAppCurrentUser currentUser)
    : IRequestHandler<GetDoctorProfileQuery, GetDoctorProfileQueryDto>
{
    public async Task<GetDoctorProfileQueryDto> Handle(GetDoctorProfileQuery request, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            throw new MarketBusinessRuleException("auth.not_authenticated", "User is not authenticated.");

        var doctor = await ctx.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == currentUser.UserId.Value, ct);

        if (doctor is null)
            throw new MarketNotFoundException("Doctor profile not found.");

        return new GetDoctorProfileQueryDto
        {
            FirstName = doctor.FirstName,
            LastName = doctor.LastName,
            PhoneNumber = doctor.PhoneNumber ?? "N/A",
            Specialization = doctor.Specialization,
            LicenseNumber = doctor.LicenseNumber,
            BirthDate = doctor.BirthDate,
            Address = doctor.Address ?? "N/A",
            Email = doctor.User.Email
        };
    }
}
