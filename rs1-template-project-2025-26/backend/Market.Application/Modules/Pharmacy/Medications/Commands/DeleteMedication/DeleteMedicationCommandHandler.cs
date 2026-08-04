using Market.Application.Modules.Pharmacy.Medications.Commands.DeleteMedication;

public sealed class DeleteMedicationCommandHandler(IAppDbContext ctx, IPharmacyAnalyticsService analytics)
    : IRequestHandler<DeleteMedicationCommand>
{
    public async Task Handle(DeleteMedicationCommand request, CancellationToken ct)
    {
        var medication = await ctx.Medications
            .FirstOrDefaultAsync(m => m.Id == request.Id && !m.IsDeleted, ct);

        if (medication is null)
            throw new MarketNotFoundException("Medication not found.");

        medication.IsActive = false;
        medication.ModifiedAtUtc = DateTime.UtcNow;

        await ctx.SaveChangesAsync(ct);
        analytics.InvalidateAnalyticsCache();
    }
}
