using eBolnica.Application.Modules.Admin.Common;
using eBolnica.Application.Modules.Admin.Users.Commands.DeleteUser;

public sealed class DeleteUserCommandHandler(IAppDbContext ctx, IAppCurrentUser currentUser)
    : IRequestHandler<DeleteUserCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(DeleteUserCommand request, CancellationToken ct)
    {
        if (currentUser.UserId == request.AppUserId)
            throw new eBolnicaBusinessRuleException("user.self_delete", "You cannot delete your own account.");

        var user = await ctx.Users.FirstOrDefaultAsync(u => u.Id == request.AppUserId, ct)
            ?? throw new eBolnicaNotFoundException("User not found.");

        var doctor = await ctx.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id, ct);
        if (doctor is not null)
            ctx.Doctors.Remove(doctor);

        var patient = await ctx.Patients
            .Include(p => p.MedicalRecord)
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct);

        if (patient is not null)
        {
            if (patient.MedicalRecord is not null)
                ctx.MedicalRecords.Remove(patient.MedicalRecord);

            ctx.Patients.Remove(patient);
        }

        var pharmacist = await ctx.Pharmacists.FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        if (pharmacist is not null)
            ctx.Pharmacists.Remove(pharmacist);

        ctx.Users.Remove(user);
        await ctx.SaveChangesAsync(ct);

        return new MessageResponseDto { Message = "User deleted successfully." };
    }
}
