using eBolnica.Application.Modules.MedicalRecord.Queries.GetMedicalRecordByPatientId;

public sealed class GetMedicalRecordByPatientIdQueryHandler(IAppDbContext ctx, IAppCurrentUser currentUser)
    : IRequestHandler<GetMedicalRecordByPatientIdQuery, GetMedicalRecordByPatientIdQueryDto>
{
    public async Task<GetMedicalRecordByPatientIdQueryDto> Handle(
        GetMedicalRecordByPatientIdQuery request, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            throw new eBolnicaBusinessRuleException("auth.not_authenticated", "User is not authenticated.");

        var doctor = await ctx.Doctors
            .FirstOrDefaultAsync(d => d.UserId == currentUser.UserId.Value, ct);

        if (doctor is null)
            throw new eBolnicaNotFoundException("Doctor profile not found.");

        var patient = await ctx.Patients
            .Include(p => p.MedicalRecord)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == request.PatientId && p.DoctorId == doctor.Id, ct);

        if (patient is null)
            throw new eBolnicaNotFoundException("Patient not found or access denied.");

        if (patient.MedicalRecord is null)
            throw new eBolnicaNotFoundException("Medical record not found.");

        var reports = await ctx.MedicalReports
            .Where(r => r.MedicalRecordId == patient.MedicalRecord.Id)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new MedicalReportItemDto
            {
                Id = r.Id,
                DoctorId = r.DoctorId,
                CreatedAt = r.CreatedAtUtc,
                Diagnosis = r.Diagnosis,
                Therapy = r.Therapy,
                Symptoms = r.Symptoms,
                Description = r.Description
            })
            .ToListAsync(ct);

        var appointments = await ctx.Appointments
            .Where(a => a.PatientId == patient.Id)
            .OrderByDescending(a => a.ScheduledAtUtc)
            .Select(a => new AppointmentItemDto
            {
                Id = a.Id,
                ScheduledAtUtc = a.ScheduledAtUtc,
                DurationMinutes = a.DurationMinutes,
                Reason = a.Reason,
                Status = a.Status,
                Notes = a.Notes
            })
            .ToListAsync(ct);

        var hospitalizations = await ctx.Hospitalizations
            .Where(h => h.PatientId == patient.Id)
            .OrderByDescending(h => h.AdmittedAtUtc)
            .Select(h => new HospitalizationItemDto
            {
                Id = h.Id,
                AdmittedAtUtc = h.AdmittedAtUtc,
                DischargedAtUtc = h.DischargedAtUtc,
                Ward = h.Ward,
                RoomNumber = h.RoomNumber,
                AdmissionReason = h.AdmissionReason,
                Status = h.Status
            })
            .ToListAsync(ct);

        var diagnoses = await ctx.ClinicalDiagnoses
            .Where(d => d.PatientId == patient.Id)
            .OrderByDescending(d => d.DiagnosedAtUtc)
            .Select(d => new ClinicalDiagnosisItemDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                Description = d.Description,
                DiagnosedAtUtc = d.DiagnosedAtUtc,
                MedicalReportId = d.MedicalReportId
            })
            .ToListAsync(ct);

        var allergies = await ctx.PatientAllergies
            .Where(a => a.PatientId == patient.Id)
            .Select(a => new PatientAllergyItemDto
            {
                Id = a.Id,
                Allergen = a.Allergen,
                Severity = a.Severity,
                Reaction = a.Reaction
            })
            .ToListAsync(ct);

        return new GetMedicalRecordByPatientIdQueryDto
        {
            Id = patient.MedicalRecord.Id,
            PatientId = patient.MedicalRecord.PatientId,
            RecordNumber = patient.MedicalRecord.RecordNumber,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            PhoneNumber = patient.PhoneNumber,
            Address = patient.Address,
            IsAdmitted = patient.IsAdmitted,
            BloodType = patient.BloodType,
            Email = patient.User.Email,
            Reports = reports,
            Appointments = appointments,
            Hospitalizations = hospitalizations,
            Diagnoses = diagnoses,
            Allergies = allergies
        };
    }
}
