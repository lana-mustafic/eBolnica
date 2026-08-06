using eBolnica.Application.Modules.Pharmacy.Medications;
using eBolnica.Application.Modules.Pharmacy.Medications.Commands.DeleteMedication;

public sealed class DeleteMedicationCommandHandler(IAppDbContext ctx, IPharmacyAnalyticsService analytics)
    : IRequestHandler<DeleteMedicationCommand>
{
    public async Task Handle(DeleteMedicationCommand request, CancellationToken ct)
    {
        var medication = await ctx.Medications
            .FirstOrDefaultAsync(m => m.Id == request.Id && !m.IsDeleted, ct);

        if (medication is null)
            throw new eBolnicaNotFoundException("Medication not found.");

        await MedicationWorkflowGuard.EnsureNoPendingPrescriptionsAsync(ctx, request.Id, ct);
        await MedicationWorkflowGuard.EnsureNoPrescriptionHistoryAsync(ctx, request.Id, ct);

        medication.IsActive = false;
        medication.IsDeleted = true;
        medication.ModifiedAtUtc = DateTime.UtcNow;

        await ctx.SaveChangesAsync(ct);
        analytics.InvalidateAnalyticsCache();
    }
}
