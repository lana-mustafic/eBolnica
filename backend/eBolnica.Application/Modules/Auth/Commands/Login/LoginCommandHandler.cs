using eBolnica.Application.Modules.Auth.Commands.Login;
using eBolnica.Domain.Entities.Identity;

public sealed class LoginCommandHandler(
    IAppDbContext ctx,
    IJwtTokenService jwt,
    IPasswordHasher<eBolnicaUserEntity> hasher)
    : IRequestHandler<LoginCommand, LoginCommandDto>
{
    public async Task<LoginCommandDto> Handle(LoginCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await ctx.Users
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email && x.IsEnabled && !x.IsDeleted, ct)
            ?? throw new eBolnicaNotFoundException("Korisnik nije pronađen ili je onemogućen.");

        var verify = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verify == PasswordVerificationResult.Failed)
            throw new eBolnicaConflictException("Pogrešni kredencijali.");

        await EnsureRegistrationApprovedAsync(user, ct);

        var tokens = jwt.IssueTokens(user);

        ctx.RefreshTokens.Add(new RefreshTokenEntity
        {
            TokenHash = tokens.RefreshTokenHash,
            ExpiresAtUtc = tokens.RefreshTokenExpiresAtUtc,
            UserId = user.Id,
            Fingerprint = request.Fingerprint
        });

        await ctx.SaveChangesAsync(ct);

        return new LoginCommandDto
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshTokenRaw,
            ExpiresAtUtc = tokens.RefreshTokenExpiresAtUtc,
            AccessExpiresAtUtc = tokens.AccessTokenExpiresAtUtc,
            RefreshExpiresAtUtc = tokens.RefreshTokenExpiresAtUtc
        };
    }

    private async Task EnsureRegistrationApprovedAsync(eBolnicaUserEntity user, CancellationToken ct)
    {
        if (user.UserType == UserTypes.Doctor)
        {
            var doctor = await ctx.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id, ct);
            if (doctor is null || !string.Equals(doctor.RegistrationStatus, "Approved", StringComparison.OrdinalIgnoreCase))
                throw new eBolnicaBusinessRuleException("auth.not_approved", "Your account is not approved.");
        }

        if (user.UserType == UserTypes.Patient)
        {
            var patient = await ctx.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
            if (patient is null || !string.Equals(patient.RegistrationStatus, "Approved", StringComparison.OrdinalIgnoreCase))
                throw new eBolnicaBusinessRuleException("auth.not_approved", "Your account is not approved.");
        }
    }
}
