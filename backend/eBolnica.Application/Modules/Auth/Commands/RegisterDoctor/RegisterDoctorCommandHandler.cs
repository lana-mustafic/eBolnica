using eBolnica.Application.Modules.Auth.Commands.RegisterDoctor;
using eBolnica.Domain.Entities.Clinical;
using eBolnica.Domain.Entities.Identity;

public sealed class RegisterDoctorCommandHandler(
    IAppDbContext ctx,
    IPasswordHasher<eBolnicaUserEntity> hasher)
    : IRequestHandler<RegisterDoctorCommand, RegisterDoctorCommandDto>
{
    public async Task<RegisterDoctorCommandDto> Handle(RegisterDoctorCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var license = request.LicenseNumber.Trim();

        if (await ctx.Users.AnyAsync(x => x.Email.ToLower() == email, ct))
            throw new eBolnicaConflictException("Email is already registered.");

        if (await ctx.Doctors.AnyAsync(x => x.LicenseNumber == license, ct))
            throw new eBolnicaConflictException("License Number is already in use.");

        var user = new eBolnicaUserEntity
        {
            Email = email,
            Firstname = request.FirstName.Trim(),
            Lastname = request.LastName.Trim(),
            LicenseNumber = license,
            PasswordHash = hasher.HashPassword(null!, request.Password),
            UserType = UserTypes.Doctor,
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        ctx.Users.Add(user);
        await ctx.SaveChangesAsync(ct);

        ctx.Doctors.Add(new DoctorEntity
        {
            UserId = user.Id,
            FirstName = user.Firstname,
            LastName = user.Lastname,
            LicenseNumber = license,
            RegistrationStatus = "Pending",
            BirthDate = request.DateOfBirth,
            Gender = request.Gender,
            CreatedAtUtc = DateTime.UtcNow
        });

        await ctx.SaveChangesAsync(ct);

        return new RegisterDoctorCommandDto();
    }
}
