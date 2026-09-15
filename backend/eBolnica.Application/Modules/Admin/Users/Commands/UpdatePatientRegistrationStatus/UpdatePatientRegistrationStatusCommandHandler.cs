using eBolnica.Application.Modules.Admin.Common;
using eBolnica.Application.Modules.Admin.Users;
using eBolnica.Application.Modules.Admin.Users.Commands.UpdatePatientRegistrationStatus;
using eBolnica.Application.Modules.Auth;

public sealed class UpdatePatientRegistrationStatusCommandHandler(IAppDbContext ctx)
    : IRequestHandler<UpdatePatientRegistrationStatusCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(UpdatePatientRegistrationStatusCommand request, CancellationToken ct)
    {
        var patient = await ctx.Patients.FirstOrDefaultAsync(p => p.UserId == request.AppUserId, ct)
            ?? throw new eBolnicaNotFoundException("Patient not found.");

        if (RegistrationApprovalGuard.IsApprovedStatus(request.RegistrationStatus))
        {
            var doctor = await PatientDoctorAssignment.RequireAssignableDoctorAsync(ctx, request.DoctorId, ct);
            patient.DoctorId = doctor.Id;
        }

        patient.RegistrationStatus = request.RegistrationStatus;
        if (!RegistrationApprovalGuard.IsApprovedStatus(request.RegistrationStatus))
            await RegistrationApprovalGuard.RevokeActiveRefreshTokensAsync(ctx, request.AppUserId, DateTime.UtcNow, ct);

        await ctx.SaveChangesAsync(ct);

        return new MessageResponseDto { Message = "Patient registration status updated successfully." };
    }
}
