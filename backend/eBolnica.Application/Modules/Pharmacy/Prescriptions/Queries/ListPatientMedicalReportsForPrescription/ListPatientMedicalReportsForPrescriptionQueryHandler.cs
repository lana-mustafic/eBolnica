namespace eBolnica.Application.Modules.Pharmacy.Prescriptions.Queries.ListPatientMedicalReportsForPrescription;

public sealed class ListPatientMedicalReportsForPrescriptionQueryHandler(IAppDbContext ctx, IAppCurrentUser currentUser)
    : IRequestHandler<ListPatientMedicalReportsForPrescriptionQuery, IReadOnlyList<PrescriptionFormMedicalReportDto>>
{
    public async Task<IReadOnlyList<PrescriptionFormMedicalReportDto>> Handle(
        ListPatientMedicalReportsForPrescriptionQuery request,
        CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            throw new eBolnicaBusinessRuleException("auth.not_authenticated", "User is not authenticated.");

        var pharmacist = await ctx.Pharmacists
            .AnyAsync(p => p.UserId == currentUser.UserId.Value, ct);

        if (!pharmacist)
            throw new eBolnicaNotFoundException("Pharmacist profile not found.");

        var patientExists = await ctx.Patients.AnyAsync(p => p.Id == request.PatientId, ct);
        if (!patientExists)
            throw new eBolnicaNotFoundException("Patient not found.");

        return await ctx.MedicalReports
            .AsNoTracking()
            .Include(r => r.Doctor)
            .Where(r => r.MedicalRecord.PatientId == request.PatientId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new PrescriptionFormMedicalReportDto
            {
                Id = r.Id,
                CreatedAt = r.CreatedAtUtc,
                Diagnosis = r.Diagnosis,
                DoctorFirstName = r.Doctor.FirstName,
                DoctorLastName = r.Doctor.LastName,
                DoctorSpecialization = r.Doctor.Specialization
            })
            .ToListAsync(ct);
    }
}
