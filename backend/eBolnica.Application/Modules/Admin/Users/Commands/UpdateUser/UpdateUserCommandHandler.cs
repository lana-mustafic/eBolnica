using eBolnica.Application.Modules.Admin.Common;
using eBolnica.Application.Modules.Admin.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler(IAppDbContext ctx)
    : IRequestHandler<UpdateUserCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(UpdateUserCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await ctx.Users.FirstOrDefaultAsync(u => u.Id == request.AppUserId, ct)
            ?? throw new eBolnicaNotFoundException("User not found.");

        if (await ctx.Users.AnyAsync(u => u.Id != user.Id && u.Email.ToLower() == email, ct))
            throw new eBolnicaConflictException("Email is already in use.");

        user.Firstname = request.FirstName.Trim();
        user.Lastname = request.LastName.Trim();
        user.Email = email;

        var doctor = await ctx.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id, ct);
        if (doctor is not null)
        {
            doctor.FirstName = user.Firstname;
            doctor.LastName = user.Lastname;
        }

        var patient = await ctx.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        if (patient is not null)
        {
            patient.FirstName = user.Firstname;
            patient.LastName = user.Lastname;
        }

        var pharmacist = await ctx.Pharmacists.FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        if (pharmacist is not null)
        {
            pharmacist.FirstName = user.Firstname;
            pharmacist.LastName = user.Lastname;
        }

        await ctx.SaveChangesAsync(ct);

        return new MessageResponseDto { Message = "User updated successfully." };
    }
}
