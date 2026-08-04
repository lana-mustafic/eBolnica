using Market.Application.Modules.MedicalRecord.Commands.CreateMedicalReport;

public sealed class CreateMedicalReportCommandHandler(IAppDbContext ctx, IAppCurrentUser currentUser)
    : IRequestHandler<CreateMedicalReportCommand, CreateMedicalReportCommandDto>
{
    public async Task<CreateMedicalReportCommandDto> Handle(
        CreateMedicalReportCommand request, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            throw new MarketBusinessRuleException("auth.not_authenticated", "User is not authenticated.");

        var doctor = await ctx.Doctors
            .FirstOrDefaultAsync(d => d.UserId == currentUser.UserId.Value, ct);

        if (doctor is null)
            throw new MarketNotFoundException("Doctor profile not found.");

        var medicalRecord = await ctx.MedicalRecords
            .Include(mr => mr.Patient)
            .FirstOrDefaultAsync(
                mr => mr.Id == request.MedicalRecordId && mr.Patient.DoctorId == doctor.Id, ct);

        if (medicalRecord is null)
            throw new MarketNotFoundException("Medical record not found or access denied.");

        var report = new MedicalReportEntity
        {
            MedicalRecordId = request.MedicalRecordId,
            DoctorId = doctor.Id,
            Symptoms = request.Symptoms?.Trim(),
            Diagnosis = request.Diagnosis?.Trim(),
            Therapy = request.Therapy?.Trim(),
            Description = request.Description?.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        ctx.MedicalReports.Add(report);
        await ctx.SaveChangesAsync(ct);

        return new CreateMedicalReportCommandDto { Id = report.Id };
    }
}
