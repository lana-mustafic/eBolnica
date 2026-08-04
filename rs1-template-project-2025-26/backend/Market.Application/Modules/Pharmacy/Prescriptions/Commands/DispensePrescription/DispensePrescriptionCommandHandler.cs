using Market.Application.Modules.Pharmacy.Prescriptions;
using Market.Domain.Entities.Pharmacy;

namespace Market.Application.Modules.Pharmacy.Prescriptions.Commands.DispensePrescription;

public sealed class DispensePrescriptionCommandHandler(IAppDbContext ctx, IAppCurrentUser currentUser, IPharmacyAnalyticsService analytics)
    : IRequestHandler<DispensePrescriptionCommand, PrescriptionDto>
{
    public async Task<PrescriptionDto> Handle(DispensePrescriptionCommand request, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            throw new MarketBusinessRuleException("auth.not_authenticated", "User is not authenticated.");

        var pharmacist = await ctx.Pharmacists
            .FirstOrDefaultAsync(p => p.UserId == currentUser.UserId.Value, ct);

        if (pharmacist is null)
            throw new MarketNotFoundException("Pharmacist profile not found.");

        await using var transaction = await ctx.Database.BeginTransactionAsync(ct);
        try
        {
            var prescription = await ctx.Prescriptions
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, ct);

            if (prescription is null)
                throw new MarketNotFoundException("Prescription not found.");

            if (!string.Equals(prescription.Status, PrescriptionStatuses.Pending, StringComparison.OrdinalIgnoreCase))
                throw new MarketBusinessRuleException(
                    "prescription.already_processed",
                    $"Prescription is already {prescription.Status}. Only pending prescriptions can be dispensed.");

            if (prescription.Items.Count == 0)
                throw new MarketBusinessRuleException("prescription.no_items", "Prescription has no items to dispense.");

            var requiredByMedication = prescription.Items
                .GroupBy(i => i.MedicationId)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

            var medicationIds = requiredByMedication.Keys.ToList();
            var medications = await ctx.Medications
                .Where(m => medicationIds.Contains(m.Id))
                .ToListAsync(ct);

            if (medications.Count != medicationIds.Count)
                throw new MarketBusinessRuleException("prescription.medication_missing", "One or more medications could not be found.");

            var todayUtc = DateTime.UtcNow.Date;
            foreach (var medication in medications)
            {
                var required = requiredByMedication[medication.Id];

                if (!medication.IsActive)
                    throw new MarketBusinessRuleException("prescription.medication_inactive", $"Medication {medication.Name} is inactive.");

                if (medication.ExpiryDate.HasValue && medication.ExpiryDate.Value.Date < todayUtc)
                    throw new MarketBusinessRuleException("prescription.medication_expired", $"Medication {medication.Name} is expired.");

                if (medication.StockQuantity < required)
                    throw new MarketBusinessRuleException(
                        "prescription.insufficient_stock",
                        $"Insufficient stock for {medication.Name}. Available: {medication.StockQuantity}, Required: {required}.");
            }

            var now = DateTime.UtcNow;
            foreach (var medication in medications)
            {
                medication.StockQuantity -= requiredByMedication[medication.Id];
                medication.ModifiedAtUtc = now;
            }

            prescription.Status = PrescriptionStatuses.Dispensed;
            prescription.PharmacistId = pharmacist.Id;
            prescription.DispensedDate = request.DispensedDate ?? now;
            prescription.ModifiedAtUtc = now;

            await ctx.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        var result = await ctx.Prescriptions
            .AsNoTracking()
            .WithDetails()
            .FirstAsync(p => p.Id == request.PrescriptionId, ct);

        analytics.InvalidateAnalyticsCache();
        return PrescriptionMapping.MapToDto(result);
    }
}