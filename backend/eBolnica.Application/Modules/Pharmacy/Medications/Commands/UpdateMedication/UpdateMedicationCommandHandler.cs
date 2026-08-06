using eBolnica.Application.Common;
using eBolnica.Application.Modules.Pharmacy.Medications;
using eBolnica.Application.Modules.Pharmacy.Medications.Commands.UpdateMedication;
using eBolnica.Domain.Entities.Pharmacy;
using Microsoft.EntityFrameworkCore;

public sealed class UpdateMedicationCommandHandler(IAppDbContext ctx, IPharmacyAnalyticsService analytics)
    : IRequestHandler<UpdateMedicationCommand, MedicationDto>
{
    public async Task<MedicationDto> Handle(UpdateMedicationCommand request, CancellationToken ct)
    {
        var medication = await ctx.Medications
            .FirstOrDefaultAsync(m => m.Id == request.Id && !m.IsDeleted, ct);

        if (medication is null || (!medication.IsActive && !request.IsActive))
            throw new eBolnicaNotFoundException("Medication not found.");

        var normalized = MedicationEntity.NormalizeName(request.Name);
        if (await ctx.Medications.AnyAsync(
                m => !m.IsDeleted && m.NormalizedName == normalized && m.Id != request.Id, ct))
            throw new eBolnicaConflictException("A medication with this name already exists.");

        if (!request.IsActive && medication.IsActive)
            await MedicationWorkflowGuard.EnsureNoPendingPrescriptionsAsync(ctx, request.Id, ct);

        medication.Name = request.Name.Trim();
        medication.NormalizedName = normalized;
        medication.GenericName = request.GenericName?.Trim();
        medication.Description = request.Description?.Trim();
        medication.Manufacturer = request.Manufacturer?.Trim();
        medication.Price = request.Price;
        medication.StockQuantity = request.StockQuantity;
        medication.MinimumStockLevel = request.MinimumStockLevel;
        medication.ExpiryDate = request.ExpiryDate;
        medication.BatchNumber = request.BatchNumber?.Trim();
        medication.IsActive = request.IsActive;
        medication.RequiresPrescription = request.RequiresPrescription;
        medication.Category = request.Category.Trim();
        medication.DosageForm = request.DosageForm?.Trim();
        medication.Strength = request.Strength?.Trim();
        medication.ModifiedAtUtc = DateTime.UtcNow;

        if (request.RowVersion is { Length: > 0 }
            && !request.RowVersion.AsSpan().SequenceEqual(medication.RowVersion))
        {
            throw new eBolnicaConflictException(
                "Medication was modified by another operation. Refresh and try again.");
        }

        try
        {
            await ctx.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new eBolnicaConflictException(
                "Medication was modified by another operation. Refresh and try again.");
        }
        catch (DbUpdateException ex) when (DbUpdateExceptionHelper.IsUniqueConstraintViolation(ex))
        {
            throw new eBolnicaConflictException("A medication with this name already exists.");
        }

        analytics.InvalidateAnalyticsCache();
        return new MedicationDto
        {
            Id = medication.Id,
            Name = medication.Name,
            GenericName = medication.GenericName,
            Description = medication.Description,
            Manufacturer = medication.Manufacturer,
            Price = medication.Price,
            StockQuantity = medication.StockQuantity,
            MinimumStockLevel = medication.MinimumStockLevel,
            ExpiryDate = medication.ExpiryDate,
            BatchNumber = medication.BatchNumber,
            IsActive = medication.IsActive,
            RequiresPrescription = medication.RequiresPrescription,
            Category = medication.Category,
            DosageForm = medication.DosageForm,
            Strength = medication.Strength,
            CreatedAt = medication.CreatedAtUtc,
            UpdatedAt = medication.ModifiedAtUtc,
            RowVersion = medication.RowVersion
        };
    }
}
