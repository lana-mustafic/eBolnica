using eBolnica.Domain.Entities.Identity;

namespace eBolnica.Application.Modules.Auth;

internal static class RegistrationApprovalGuard
{
    public static bool IsApprovedStatus(string? status) =>
        string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase);

    public static async Task EnsureApprovedAsync(
        IAppDbContext ctx,
        eBolnicaUserEntity user,
        CancellationToken ct)
    {
        if (user.UserType == UserTypes.Doctor)
        {
            var doctor = await ctx.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id, ct);
            if (doctor is null || !IsApprovedStatus(doctor.RegistrationStatus))
                throw new eBolnicaBusinessRuleException("auth.not_approved", "Your account is not approved.");
        }

        if (user.UserType == UserTypes.Patient)
        {
            var patient = await ctx.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
            if (patient is null || !IsApprovedStatus(patient.RegistrationStatus))
                throw new eBolnicaBusinessRuleException("auth.not_approved", "Your account is not approved.");
        }
    }

    public static async Task RevokeActiveRefreshTokensAsync(
        IAppDbContext ctx,
        int userId,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var tokens = await ctx.RefreshTokens
            .Where(x => x.UserId == userId && !x.IsRevoked && !x.IsDeleted)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAtUtc = nowUtc;
        }
    }
}
