using eBolnica.Application.Modules.Auth.Commands.RegisterPatient;
using eBolnica.Domain.Entities.Clinical;
using eBolnica.Domain.Entities.Identity;

public sealed class RegisterPatientCommandHandler(
    IAppDbContext ctx,
    IPasswordHasher<eBolnicaUserEntity> hasher)
    : IRequestHandler<RegisterPatientCommand, RegisterPatientCommandDto>
{
    public async Task<RegisterPatientCommandDto> Handle(RegisterPatientCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await ctx.Users.AnyAsync(x => x.Email.ToLower() == email, ct))
            throw new eBolnicaConflictException("Email is already registered.");

        var user = new eBolnicaUserEntity
        {
            Email = email,
            Firstname = request.FirstName.Trim(),
            Lastname = request.LastName.Trim(),
            PasswordHash = hasher.HashPassword(null!, request.Password),
            UserType = UserTypes.Patient,
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        ctx.Users.Add(user);
        await ctx.SaveChangesAsync(ct);

        var patient = new PatientEntity
        {
            UserId = user.Id,
            FirstName = user.Firstname,
            LastName = user.Lastname,
            RegistrationStatus = "Pending",
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            CreatedAtUtc = DateTime.UtcNow
        };

        ctx.Patients.Add(patient);
        await ctx.SaveChangesAsync(ct);

        ctx.MedicalRecords.Add(new MedicalRecordEntity
        {
            PatientId = patient.Id,
            RecordNumber = $"MR-{DateTime.UtcNow:yyyy}-{patient.Id}",
            CreatedAtUtc = DateTime.UtcNow
        });

        await ctx.SaveChangesAsync(ct);

        return new RegisterPatientCommandDto();
    }
}
