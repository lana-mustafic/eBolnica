using eBolnica.Application.Modules.Pharmacy.Prescriptions;

namespace eBolnica.Application.Modules.Pharmacy.Prescriptions.Commands.CreatePharmacyPrescription;
public sealed class CreatePharmacyPrescriptionCommandHandler(
    IAppDbContext ctx,
    IAppCurrentUser currentUser,
    IPrescriptionCreationService prescriptionCreationService)
    : IRequestHandler<CreatePharmacyPrescriptionCommand, PrescriptionDto>
{
    public async Task<PrescriptionDto> Handle(CreatePharmacyPrescriptionCommand request, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            throw new eBolnicaBusinessRuleException("auth.not_authenticated", "User is not authenticated.");

        var pharmacist = await ctx.Pharmacists
            .FirstOrDefaultAsync(p => p.UserId == currentUser.UserId.Value, ct);

        if (pharmacist is null)
            throw new eBolnicaNotFoundException("Pharmacist profile not found.");

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
