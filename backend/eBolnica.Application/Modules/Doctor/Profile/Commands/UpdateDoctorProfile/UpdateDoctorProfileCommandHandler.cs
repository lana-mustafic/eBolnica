using eBolnica.Application.Modules.Doctor.Profile.Commands.UpdateDoctorProfile;

public sealed class UpdateDoctorProfileCommandHandler(IAppDbContext ctx, IAppCurrentUser currentUser)
    : IRequestHandler<UpdateDoctorProfileCommand, UpdateDoctorProfileCommandDto>
{
    public async Task<UpdateDoctorProfileCommandDto> Handle(UpdateDoctorProfileCommand request, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            throw new eBolnicaBusinessRuleException("auth.not_authenticated", "User is not authenticated.");

        var doctor = await ctx.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == currentUser.UserId.Value, ct);

        if (doctor is null)
            throw new eBolnicaNotFoundException("Doctor profile not found.");

        doctor.FirstName = request.FirstName.Trim();
        doctor.LastName = request.LastName.Trim();
        doctor.PhoneNumber = request.PhoneNumber?.Trim();
        doctor.Address = request.Address?.Trim();
        doctor.Specialization = request.Specialization?.Trim();
        doctor.User.Firstname = doctor.FirstName;
        doctor.User.Lastname = doctor.LastName;
        doctor.ModifiedAtUtc = DateTime.UtcNow;

        await ctx.SaveChangesAsync(ct);

        return new UpdateDoctorProfileCommandDto
        {
            FirstName = doctor.FirstName,
            LastName = doctor.LastName,
            PhoneNumber = doctor.PhoneNumber,
            Address = doctor.Address,
            Specialization = doctor.Specialization
        };
    }
}
