using eBolnica.Application.Modules.Admin.Common;
using eBolnica.Application.Modules.Admin.Users.Commands.CreateUser;
using eBolnica.Domain.Entities.Clinical;
using eBolnica.Domain.Entities.Identity;

public sealed class CreateUserCommandHandler(
    IAppDbContext ctx,
    IPasswordHasher<eBolnicaUserEntity> hasher)
    : IRequestHandler<CreateUserCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(CreateUserCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await ctx.Users.AnyAsync(u => u.Email.ToLower() == email, ct))
            throw new eBolnicaConflictException("User with this email already exists.");

        DoctorEntity? assignedDoctor = null;
        if (request.UserType == UserTypes.Patient)
        {
            assignedDoctor = await ctx.Doctors.FirstOrDefaultAsync(d => d.Id == request.DoctorId, ct)
                ?? throw new eBolnicaBusinessRuleException("doctor.not_found", "Selected doctor not found.");
        }

        var user = new eBolnicaUserEntity
        {
            Email = email,
            Firstname = request.FirstName.Trim(),
            Lastname = request.LastName.Trim(),
            PasswordHash = hasher.HashPassword(null!, request.Password),
            UserType = request.UserType,
            LicenseNumber = request.LicenseNumber,
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        ctx.Users.Add(user);
        await ctx.SaveChangesAsync(ct);

        switch (request.UserType)
        {
            case UserTypes.Doctor:
                var doctorLicense = request.LicenseNumber?.Trim();
                if (string.IsNullOrWhiteSpace(doctorLicense))
                    doctorLicense = $"DOC-{user.Id}";

                if (await ctx.Doctors.AnyAsync(d => d.LicenseNumber == doctorLicense, ct))
                    throw new eBolnicaConflictException("License number is already in use.");

                ctx.Doctors.Add(new DoctorEntity
                {
                    UserId = user.Id,
                    FirstName = user.Firstname,
                    LastName = user.Lastname,
                    LicenseNumber = doctorLicense,
                    RegistrationStatus = "Pending",
                    CreatedAtUtc = DateTime.UtcNow
                });
                user.LicenseNumber = doctorLicense;
                break;

            case UserTypes.Patient:
                var patient = new PatientEntity
                {
                    UserId = user.Id,
                    FirstName = user.Firstname,
                    LastName = user.Lastname,
                    DoctorId = assignedDoctor!.Id,
                    RegistrationStatus = "Approved",
                    CreatedAtUtc = DateTime.UtcNow
                };
                ctx.Patients.Add(patient);
                await ctx.SaveChangesAsync(ct);

                ctx.MedicalRecords.Add(new MedicalRecordEntity
                {
                    PatientId = patient.Id,
                    RecordNumber = $"MR-{DateTime.UtcNow:yyyyMMdd}-{user.Id}",
                    CreatedAtUtc = DateTime.UtcNow
                });
                break;

            case UserTypes.Pharmacist:
                var pharmacistLicense = request.LicenseNumber?.Trim();
                if (string.IsNullOrWhiteSpace(pharmacistLicense))
                    pharmacistLicense = $"PH-{user.Id}";

                if (await ctx.Pharmacists.AnyAsync(p => p.LicenseNumber == pharmacistLicense, ct))
                    throw new eBolnicaConflictException("License number is already in use.");

                ctx.Pharmacists.Add(new PharmacistEntity
                {
                    UserId = user.Id,
                    FirstName = user.Firstname,
                    LastName = user.Lastname,
                    LicenseNumber = pharmacistLicense,
                    HireDate = DateTime.UtcNow,
                    CreatedAtUtc = DateTime.UtcNow
                });
                user.LicenseNumber = pharmacistLicense;
                break;
        }

        await ctx.SaveChangesAsync(ct);

        return new MessageResponseDto { Message = "User created successfully." };
    }
}
