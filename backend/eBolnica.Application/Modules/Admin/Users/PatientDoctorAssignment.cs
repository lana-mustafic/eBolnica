using eBolnica.Domain.Entities.Clinical;

namespace eBolnica.Application.Modules.Admin.Users;

internal static class PatientDoctorAssignment
{
    public static async Task<DoctorEntity> RequireAssignableDoctorAsync(
        IAppDbContext ctx,
        int? doctorId,
        CancellationToken ct)
    {
        if (doctorId is null or <= 0)
            throw new eBolnicaBusinessRuleException(
                "patient.doctor_required",
                "An active approved doctor must be assigned before the patient can be approved.");

        var doctor = await ctx.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId.Value && !d.IsDeleted, ct)
            ?? throw new eBolnicaBusinessRuleException("doctor.not_found", "Selected doctor not found.");

        if (!string.Equals(doctor.RegistrationStatus, "Approved", StringComparison.OrdinalIgnoreCase))
            throw new eBolnicaBusinessRuleException("doctor.not_approved", "Selected doctor is not approved.");

        var doctorUser = await ctx.Users.FirstOrDefaultAsync(u => u.Id == doctor.UserId, ct);
        if (doctorUser is null || !doctorUser.IsEnabled || doctorUser.IsDeleted)
            throw new eBolnicaBusinessRuleException("doctor.not_active", "Selected doctor is not active.");

        return doctor;
    }
}
