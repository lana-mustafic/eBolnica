using Market.Application.Modules.Pharmacy.Medications;
using Market.Application.Modules.Pharmacy.Medications.Commands.CreateMedication;
using Market.Domain.Entities.Pharmacy;

public sealed class CreateMedicationCommandHandler(IAppDbContext ctx, IPharmacyAnalyticsService analytics)
    : IRequestHandler<CreateMedicationCommand, MedicationDto>
{
    public async Task<MedicationDto> Handle(CreateMedicationCommand request, CancellationToken ct)
    {
        var normalized = MedicationEntity.NormalizeName(request.Name);
        if (await ctx.Medications.AnyAsync(m => !m.IsDeleted && m.NormalizedName == normalized, ct))
            throw new MarketConflictException("A medication with this name already exists.");

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
        await ctx.SaveChangesAsync(ct);

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
        UpdatedAt = m.ModifiedAtUtc
    };
}
