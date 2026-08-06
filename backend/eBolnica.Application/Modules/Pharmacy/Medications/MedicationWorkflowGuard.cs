using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Application.Modules.Pharmacy.Medications;

internal static class MedicationWorkflowGuard
{
    public static async Task EnsureNoPendingPrescriptionsAsync(IAppDbContext ctx, int medicationId, CancellationToken ct)
    {
        var hasPending = await ctx.PrescriptionItems
            .AsNoTracking()
            .AnyAsync(
                pi => pi.MedicationId == medicationId
                      && pi.Prescription.Status == PrescriptionStatuses.Pending,
                ct);

        if (hasPending)
        {
            throw new eBolnicaBusinessRuleException(
                "medication.pending_prescriptions",
                "Cannot delete or deactivate medication while pending prescriptions reference it.");
        }
    }

    public static async Task EnsureNoPrescriptionHistoryAsync(IAppDbContext ctx, int medicationId, CancellationToken ct)
    {
        var hasHistory = await ctx.PrescriptionItems
            .AsNoTracking()
            .AnyAsync(pi => pi.MedicationId == medicationId, ct);

        if (hasHistory)
        {
            throw new eBolnicaBusinessRuleException(
                "medication.prescription_history",
                "Cannot delete medication that appears on existing prescriptions.");
        }
    }
}
