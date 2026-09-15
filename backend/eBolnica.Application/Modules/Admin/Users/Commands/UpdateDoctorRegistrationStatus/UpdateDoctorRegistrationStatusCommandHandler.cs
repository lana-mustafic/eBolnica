using eBolnica.Application.Modules.Admin.Common;
using eBolnica.Application.Modules.Admin.Users.Commands.UpdateDoctorRegistrationStatus;
using eBolnica.Application.Modules.Auth;

public sealed class UpdateDoctorRegistrationStatusCommandHandler(IAppDbContext ctx)
    : IRequestHandler<UpdateDoctorRegistrationStatusCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(UpdateDoctorRegistrationStatusCommand request, CancellationToken ct)
    {
        var doctor = await ctx.Doctors.FirstOrDefaultAsync(d => d.UserId == request.AppUserId, ct)
            ?? throw new eBolnicaNotFoundException("Doctor not found.");

        doctor.RegistrationStatus = request.RegistrationStatus;
        if (!RegistrationApprovalGuard.IsApprovedStatus(request.RegistrationStatus))
            await RegistrationApprovalGuard.RevokeActiveRefreshTokensAsync(ctx, request.AppUserId, DateTime.UtcNow, ct);

        await ctx.SaveChangesAsync(ct);

        return new MessageResponseDto { Message = "Doctor registration status updated successfully." };
    }
}
