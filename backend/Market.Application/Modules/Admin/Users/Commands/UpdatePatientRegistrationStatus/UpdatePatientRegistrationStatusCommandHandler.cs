using Market.Application.Modules.Admin.Common;
using Market.Application.Modules.Admin.Users.Commands.UpdatePatientRegistrationStatus;

public sealed class UpdatePatientRegistrationStatusCommandHandler(IAppDbContext ctx)
    : IRequestHandler<UpdatePatientRegistrationStatusCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(UpdatePatientRegistrationStatusCommand request, CancellationToken ct)
    {
        var patient = await ctx.Patients.FirstOrDefaultAsync(p => p.UserId == request.AppUserId, ct)
            ?? throw new MarketNotFoundException("Patient not found.");

        patient.RegistrationStatus = request.RegistrationStatus;
        await ctx.SaveChangesAsync(ct);

        return new MessageResponseDto { Message = "Patient registration status updated successfully." };
    }
}
