using System.Globalization;
using System.Text;
using Market.Application.Modules.Pharmacy.Medications.Commands.CreateMedication;
using Market.Domain.Entities.Pharmacy;

namespace Market.Application.Modules.Pharmacy.Medications.Csv;

internal static class MedicationCsvImporter
{
    public static List<string[]> ParseRows(string content)
    {
        var rows = new List<string[]>();
        using var reader = new StringReader(content);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            rows.Add(ParseLine(line));
        }
        return rows;
    }

    private static string[] ParseLine(string line)
    {
        var cells = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                cells.Add(current.ToString());
                current.Clear();
            }
            else current.Append(c);
        }

        cells.Add(current.ToString());
        return cells.ToArray();
    }

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

        var name = Get("Name");
        if (string.IsNullOrWhiteSpace(name) || name.Length < 3)
        {
            error = RowError(rowNumber, "Name", name, "Name is required (3-100 characters).");
            return false;
        }

        if (!decimal.TryParse(Get("Price"), NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price <= 0)
        {
            error = RowError(rowNumber, "Price", Get("Price"), "Price must be a positive number.");
            return false;
        }

        if (!int.TryParse(Get("Stock Quantity"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var stock) || stock < 0)
        {
            error = RowError(rowNumber, "Stock Quantity", Get("Stock Quantity"), "Stock quantity must be a non-negative integer.");
            return false;
        }

        if (!int.TryParse(Get("Minimum Stock Level"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var minStock) || minStock < 0)
        {
            error = RowError(rowNumber, "Minimum Stock Level", Get("Minimum Stock Level"), "Minimum stock level must be a non-negative integer.");
            return false;
        }

        var expiryRaw = Get("Expiry Date");
        if (!DateTime.TryParseExact(expiryRaw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiry)
            || expiry.Date <= DateTime.UtcNow.Date)
        {
            error = RowError(rowNumber, "Expiry Date", expiryRaw, "Expiry date must be YYYY-MM-DD and in the future.");
            return false;
        }

        var category = Get("Category");
        if (string.IsNullOrWhiteSpace(category))
        {
            error = RowError(rowNumber, "Category", category, "Category is required.");
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

        command = new CreateMedicationCommand
        {
            Name = name,
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
            Category = category
        };

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
        Category = cmd.Category.Trim(),
        DosageForm = cmd.DosageForm?.Trim(),
        Strength = cmd.Strength?.Trim(),
        CreatedAtUtc = DateTime.UtcNow
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
