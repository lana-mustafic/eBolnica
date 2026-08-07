using eBolnica.Application.Common;
using eBolnica.Application.Modules.Pharmacy;
using eBolnica.Application.Modules.Pharmacy.Activities;
using eBolnica.Application.Modules.Pharmacy.Medications;
using eBolnica.Application.Modules.Pharmacy.Medications.Commands.CreateMedication;
using eBolnica.Domain.Entities.Pharmacy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public sealed class CreateMedicationCommandHandler(
    IAppDbContext ctx,
    IAppCurrentUser currentUser,
    IPharmacyAnalyticsService analytics,
    ILogger<CreateMedicationCommandHandler> logger)
    : IRequestHandler<CreateMedicationCommand, MedicationDto>
{
    public async Task<MedicationDto> Handle(CreateMedicationCommand request, CancellationToken ct)
    {
        var normalized = MedicationEntity.NormalizeName(request.Name);
        if (await ctx.Medications.AnyAsync(m => !m.IsDeleted && m.NormalizedName == normalized, ct))
            throw new eBolnicaConflictException("medication.duplicate_name", "A medication with this name already exists.");

        var now = DateTime.UtcNow;
        var medication = new MedicationEntity
        {
            Name = request.Name.Trim(),
            NormalizedName = normalized,
            GenericName = request.GenericName?.Trim(),
            Description = request.Description?.Trim(),
            Manufacturer = request.Manufacturer?.Trim(),
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            MinimumStockLevel = request.MinimumStockLevel,
            ExpiryDate = request.ExpiryDate,
            BatchNumber = request.BatchNumber?.Trim(),
            IsActive = request.IsActive,
            RequiresPrescription = request.RequiresPrescription,
            Category = request.Category.Trim(),
            DosageForm = request.DosageForm?.Trim(),
            Strength = request.Strength?.Trim(),
            CreatedAtUtc = now
        };

        ctx.Medications.Add(medication);
        try
        {
            await ctx.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (DbUpdateExceptionHelper.IsUniqueConstraintViolation(ex))
        {
            throw new eBolnicaConflictException("medication.duplicate_name", "A medication with this name already exists.");
        }

        MedicationStockHistoryWriter.Record(
            ctx,
            medication.Id,
            0,
            medication.StockQuantity,
            MedicationStockChangeReasons.InitialStock);

        PharmacyActivityWriter.Record(
            ctx,
            PharmacyActivityEventTypes.MedicationCreated,
            PharmacyActivityCategories.Medication,
            PharmacyActivitySeverities.Success,
            $"Dodan lijek {medication.Name}",
            currentUser.UserId,
            medicationId: medication.Id);

        await ctx.SaveChangesAsync(ct);

        PharmacyOperationLogger.MedicationCreated(logger, medication.Id, medication.Name);

        analytics.InvalidateAnalyticsCache();
        return MapToDto(medication);
    }

    private static MedicationDto MapToDto(MedicationEntity m) => new()
    {
        Id = m.Id,
        Name = m.Name,
        GenericName = m.GenericName,
        Description = m.Description,
        Manufacturer = m.Manufacturer,
        Price = m.Price,
        StockQuantity = m.StockQuantity,
        MinimumStockLevel = m.MinimumStockLevel,
        ExpiryDate = m.ExpiryDate,
        BatchNumber = m.BatchNumber,
        IsActive = m.IsActive,
        RequiresPrescription = m.RequiresPrescription,
        Category = m.Category,
        DosageForm = m.DosageForm,
        Strength = m.Strength,
        CreatedAt = m.CreatedAtUtc,
        UpdatedAt = m.ModifiedAtUtc,
        RowVersion = m.RowVersion
    };
}
