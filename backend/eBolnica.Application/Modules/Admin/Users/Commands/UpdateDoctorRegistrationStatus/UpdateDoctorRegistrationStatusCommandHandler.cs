using eBolnica.Application.Modules.Admin.Common;
using eBolnica.Application.Modules.Admin.Users.Commands.UpdateDoctorRegistrationStatus;

public sealed class UpdateDoctorRegistrationStatusCommandHandler(IAppDbContext ctx)
    : IRequestHandler<UpdateDoctorRegistrationStatusCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(UpdateDoctorRegistrationStatusCommand request, CancellationToken ct)
    {
        var doctor = await ctx.Doctors.FirstOrDefaultAsync(d => d.UserId == request.AppUserId, ct)
            ?? throw new eBolnicaNotFoundException("Doctor not found.");

        doctor.RegistrationStatus = request.RegistrationStatus;
        await ctx.SaveChangesAsync(ct);

        return new MessageResponseDto { Message = "Doctor registration status updated successfully." };
    }
}
