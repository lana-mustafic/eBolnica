using eBolnica.Application.Modules.Pharmacy.Activities;
using eBolnica.Application.Modules.Pharmacy.Prescriptions;
using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Application.Modules.Pharmacy.Prescriptions.Commands.CancelPrescription;

public sealed class CancelPrescriptionCommandHandler(IAppDbContext ctx, IAppCurrentUser currentUser)
    : IRequestHandler<CancelPrescriptionCommand, PrescriptionDto>
{
    public async Task<PrescriptionDto> Handle(CancelPrescriptionCommand request, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            throw new eBolnicaBusinessRuleException("auth.not_authenticated", "User is not authenticated.");

        var pharmacist = await ctx.Pharmacists
            .FirstOrDefaultAsync(p => p.UserId == currentUser.UserId.Value, ct);

        if (pharmacist is null)
            throw new eBolnicaNotFoundException("Pharmacist profile not found.");

        var prescription = await ctx.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, ct);

        if (prescription is null)
            throw new eBolnicaNotFoundException("Prescription not found.");

        if (!string.Equals(prescription.Status, PrescriptionStatuses.Pending, StringComparison.OrdinalIgnoreCase))
            throw new eBolnicaBusinessRuleException(
                "prescription.already_processed",
                $"Prescription is already {prescription.Status}. Only pending prescriptions can be cancelled.");

        var now = DateTime.UtcNow;
        prescription.Status = PrescriptionStatuses.Cancelled;
        prescription.ModifiedAtUtc = now;

        PharmacyActivityWriter.Record(
            ctx,
            PharmacyActivityEventTypes.PrescriptionCancelled,
            PharmacyActivityCategories.Prescription,
            PharmacyActivitySeverities.Warning,
            $"Otkazan recept {prescription.PrescriptionNumber}",
            currentUser.UserId,
            prescription.Id);

        await ctx.SaveChangesAsync(ct);

        var result = await ctx.Prescriptions
            .AsNoTracking()
            .WithDetails()
            .FirstAsync(p => p.Id == prescription.Id, ct);

        return PrescriptionMapping.MapToDto(result);
    }
}
