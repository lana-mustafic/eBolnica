using System.Globalization;
using eBolnica.Application.Modules.Pharmacy.Medications;
using eBolnica.Application.Modules.Pharmacy.Medications.Commands.CreateMedication;
using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Application.Modules.Pharmacy.Medications.Csv;

internal static class MedicationCsvImporter
{
    private static readonly CreateMedicationCommandValidator CreateValidator = new();

    public static List<string[]> ParseRows(string content) => MedicationCsvService.ParseRows(content);

    public static bool TryMapRow(
        int rowNumber,
        string[] cells,
        IReadOnlyDictionary<string, int> columns,
        out CreateMedicationCommand? command,
        out ImportRowError? error)
    {
        command = null;
        error = null;

        string Get(string header) =>
            columns.TryGetValue(header, out var idx) && idx < cells.Length
                ? cells[idx].Trim()
                : string.Empty;

        if (!decimal.TryParse(Get("Price"), NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
        {
            error = RowError(rowNumber, "Price", Get("Price"), "Price must be a valid number.");
            return false;
        }

        if (!int.TryParse(Get("Stock Quantity"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var stock))
        {
            error = RowError(rowNumber, "Stock Quantity", Get("Stock Quantity"), "Stock quantity must be an integer.");
            return false;
        }

        if (!int.TryParse(Get("Minimum Stock Level"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var minStock))
        {
            error = RowError(
                rowNumber,
                "Minimum Stock Level",
                Get("Minimum Stock Level"),
                "Minimum stock level must be an integer.");
            return false;
        }

        var expiryRaw = Get("Expiry Date");
        if (!DateTime.TryParseExact(expiryRaw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiry))
        {
            error = RowError(rowNumber, "Expiry Date", expiryRaw, "Expiry date must be YYYY-MM-DD.");
            return false;
        }

        if (!TryYesNo(Get("Requires Prescription"), out var requiresRx))
        {
            error = RowError(rowNumber, "Requires Prescription", Get("Requires Prescription"), "Use Yes or No.");
            return false;
        }

        if (!TryYesNo(Get("Active"), out var isActive))
        {
            error = RowError(rowNumber, "Active", Get("Active"), "Use Yes or No.");
            return false;
        }

        var mapped = new CreateMedicationCommand
        {
            Name = Get("Name"),
            GenericName = NullIfEmpty(Get("Generic Name")),
            Description = NullIfEmpty(Get("Description")),
            Manufacturer = NullIfEmpty(Get("Manufacturer")),
            Price = price,
            StockQuantity = stock,
            MinimumStockLevel = minStock,
            ExpiryDate = expiry,
            BatchNumber = NullIfEmpty(Get("Batch Number")),
            DosageForm = NullIfEmpty(Get("Dosage Form")),
            Strength = NullIfEmpty(Get("Strength")),
            RequiresPrescription = requiresRx,
            IsActive = isActive,
            Category = Get("Category")
        };

        var validation = CreateValidator.Validate(mapped);
        if (!validation.IsValid)
        {
            var failure = validation.Errors[0];
            var field = ToCsvField(failure.PropertyName);
            error = RowError(rowNumber, field, Get(field), failure.ErrorMessage);
            return false;
        }

        command = mapped;
        return true;
    }

    public static Dictionary<string, int> MapHeaders(string[] headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headerRow.Length; i++)
        {
            var h = headerRow[i].Trim();
            if (h.Equals("Status", StringComparison.OrdinalIgnoreCase)) continue;
            if (!string.IsNullOrEmpty(h)) map[h] = i;
        }
        return map;
    }

    public static MedicationEntity ToEntity(CreateMedicationCommand cmd) => new()
    {
        Name = cmd.Name.Trim(),
        NormalizedName = MedicationEntity.NormalizeName(cmd.Name),
        GenericName = cmd.GenericName?.Trim(),
        Description = cmd.Description?.Trim(),
        Manufacturer = cmd.Manufacturer?.Trim(),
        Price = cmd.Price,
        StockQuantity = cmd.StockQuantity,
        MinimumStockLevel = cmd.MinimumStockLevel,
        ExpiryDate = cmd.ExpiryDate,
        BatchNumber = cmd.BatchNumber?.Trim(),
        IsActive = cmd.IsActive,
        RequiresPrescription = cmd.RequiresPrescription,
        Category = MedicationCategoryAliases.ToBosnian(cmd.Category) ?? cmd.Category.Trim(),
        DosageForm = MedicationDosageFormAliases.ToBosnian(cmd.DosageForm),
        Strength = cmd.Strength?.Trim(),
        CreatedAtUtc = DateTime.UtcNow
    };

    private static string ToCsvField(string propertyName) => propertyName switch
    {
        nameof(CreateMedicationCommand.Name) => "Name",
        nameof(CreateMedicationCommand.GenericName) => "Generic Name",
        nameof(CreateMedicationCommand.Description) => "Description",
        nameof(CreateMedicationCommand.Manufacturer) => "Manufacturer",
        nameof(CreateMedicationCommand.Price) => "Price",
        nameof(CreateMedicationCommand.StockQuantity) => "Stock Quantity",
        nameof(CreateMedicationCommand.MinimumStockLevel) => "Minimum Stock Level",
        nameof(CreateMedicationCommand.ExpiryDate) => "Expiry Date",
        nameof(CreateMedicationCommand.BatchNumber) => "Batch Number",
        nameof(CreateMedicationCommand.Category) => "Category",
        nameof(CreateMedicationCommand.DosageForm) => "Dosage Form",
        nameof(CreateMedicationCommand.Strength) => "Strength",
        _ => propertyName
    };

    private static string? NullIfEmpty(string v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    private static bool TryYesNo(string raw, out bool value)
    {
        value = false;
        if (raw.Equals("yes", StringComparison.OrdinalIgnoreCase)) { value = true; return true; }
        if (raw.Equals("no", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static ImportRowError RowError(int row, string field, string? val, string reason) =>
        new() { RowNumber = row, Field = field, Value = val, Reason = reason };
}

internal sealed class ImportRowError
{
    public int RowNumber { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? Field { get; init; }
    public string? Value { get; init; }
}
