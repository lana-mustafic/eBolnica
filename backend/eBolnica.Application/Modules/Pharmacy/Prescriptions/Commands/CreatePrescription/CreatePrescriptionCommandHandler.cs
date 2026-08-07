using eBolnica.Application.Modules.Pharmacy.Prescriptions;

namespace eBolnica.Application.Modules.Pharmacy.Prescriptions.Commands.CreatePrescription;

public sealed class CreatePrescriptionCommandHandler(
    IAppDbContext ctx,
    IAppCurrentUser currentUser,
    IPrescriptionCreationService prescriptionCreationService)
    : IRequestHandler<CreatePrescriptionCommand, PrescriptionDto>
{
    public async Task<PrescriptionDto> Handle(CreatePrescriptionCommand request, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            throw new eBolnicaBusinessRuleException("auth.not_authenticated", "User is not authenticated.");

        var doctor = await ctx.Doctors
            .FirstOrDefaultAsync(d => d.UserId == currentUser.UserId.Value, ct);

        if (doctor is null)
            throw new eBolnicaNotFoundException("Doctor profile not found.");

        var medicalReport = await ctx.MedicalReports
            .Include(mr => mr.MedicalRecord)
            .FirstOrDefaultAsync(mr => mr.Id == request.MedicalReportId, ct);

        if (medicalReport is null)
            throw new eBolnicaNotFoundException("Medical report not found.");

        if (medicalReport.DoctorId != doctor.Id)
            throw new eBolnicaBusinessRuleException("prescription.report_access", "Medical report does not belong to you.");

        if (medicalReport.MedicalRecord.PatientId != request.PatientId)
            throw new eBolnicaBusinessRuleException(
                "prescription.report_patient_mismatch",
                "Medical report does not belong to the selected patient.");

        var patient = await ctx.Patients
            .FirstOrDefaultAsync(p => p.Id == request.PatientId, ct);

        if (patient is null)
            throw new eBolnicaNotFoundException("Patient not found.");

        if (patient.DoctorId != doctor.Id)
            throw new eBolnicaBusinessRuleException("prescription.patient_access", "Patient is not assigned to you.");

        return await prescriptionCreationService.CreateAsync(
            new PrescriptionCreationRequest
            {
                MedicalReportId = request.MedicalReportId,
                PatientId = request.PatientId,
                Notes = request.Notes,
                PrescriptionItems = request.PrescriptionItems
            },
            ct);
    }
}
