using eBolnica.Application.Common;
using eBolnica.Application.Modules.Pharmacy.Activities;
using eBolnica.Application.Modules.Pharmacy.Medications;
using eBolnica.Application.Modules.Pharmacy.Medications.Commands.CreateMedication;
using eBolnica.Application.Modules.Pharmacy.Medications.Commands.ImportMedicationsCsv;
using eBolnica.Application.Modules.Pharmacy.Medications.Csv;
using eBolnica.Domain.Entities.Pharmacy;
using Microsoft.EntityFrameworkCore;

public sealed class ImportMedicationsCsvCommandHandler(IAppDbContext ctx, IAppCurrentUser currentUser, IPharmacyAnalyticsService analytics)
    : IRequestHandler<ImportMedicationsCsvCommand, MedicationImportResultDto>
{
    public async Task<MedicationImportResultDto> Handle(ImportMedicationsCsvCommand request, CancellationToken ct)
    {
        var result = new MedicationImportResultDto();

        if (string.IsNullOrWhiteSpace(request.CsvContent))
            throw new eBolnicaBusinessRuleException("validation.failed", "The uploaded file is empty.");

        var rows = MedicationCsvImporter.ParseRows(request.CsvContent);
        if (rows.Count == 0)
            throw new eBolnicaBusinessRuleException("validation.failed", "The uploaded file is empty.");

        var columns = MedicationCsvImporter.MapHeaders(rows[0]);
        var dataRows = rows.Skip(1).Where(r => r.Any(c => !string.IsNullOrWhiteSpace(c))).ToList();

        result.TotalRows = dataRows.Count;
        if (dataRows.Count > MedicationCsvService.MaxImportRows)
            throw new eBolnicaBusinessRuleException(
                "import.limit_exceeded",
                $"Import is limited to {MedicationCsvService.MaxImportRows} rows.");

        if (dataRows.Count == 0)
        {
            result.Committed = true;
            return result;
        }

        var existingNames = await ctx.Medications
            .Where(m => !m.IsDeleted)
            .Select(m => m.NormalizedName)
            .ToListAsync(ct);
        var existingSet = existingNames.ToHashSet(StringComparer.Ordinal);
        var importNames = new HashSet<string>(StringComparer.Ordinal);
        var toInsert = new List<MedicationEntity>();

        for (var i = 0; i < dataRows.Count; i++)
        {
            var rowNumber = i + 2;
            if (!MedicationCsvImporter.TryMapRow(rowNumber, dataRows[i], columns, out var cmd, out var rowError))
            {
                result.Errors.Add(new MedicationImportRowErrorDto
                {
                    RowNumber = rowError!.RowNumber,
                    Field = rowError.Field,
                    Value = rowError.Value,
                    Reason = rowError.Reason
                });
                result.FailureCount++;
                continue;
            }

            var normalized = MedicationEntity.NormalizeName(cmd!.Name);
            if (existingSet.Contains(normalized) || !importNames.Add(normalized))
            {
                result.Errors.Add(new MedicationImportRowErrorDto
                {
                    RowNumber = rowNumber,
                    Field = "Name",
                    Value = cmd.Name,
                    Reason = "Medication name already exists."
                });
                result.FailureCount++;
                continue;
            }

            toInsert.Add(MedicationCsvImporter.ToEntity(cmd));
        }

        if (toInsert.Count == 0)
        {
            result.Committed = true;
            return result;
        }

        ctx.Medications.AddRange(toInsert);
        try
        {
            await ctx.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (DbUpdateExceptionHelper.IsUniqueConstraintViolation(ex))
        {
            throw new eBolnicaConflictException("One or more medication names already exist.");
        }

        foreach (var medication in toInsert)
        {
            MedicationStockHistoryWriter.Record(
                ctx,
                medication.Id,
                0,
                medication.StockQuantity,
                MedicationStockChangeReasons.Import);
        }

        PharmacyActivityWriter.Record(
            ctx,
            PharmacyActivityEventTypes.MedicationsImported,
            PharmacyActivityCategories.Medication,
            PharmacyActivitySeverities.Success,
            $"Uvezeno {toInsert.Count} lijek(ova) iz CSV-a",
            currentUser.UserId);

        await ctx.SaveChangesAsync(ct);

        analytics.InvalidateAnalyticsCache();

        result.SuccessCount = toInsert.Count;
        result.ImportedMedicationIds = toInsert.Select(m => m.Id).ToList();
        result.Committed = true;
        return result;
    }
}
