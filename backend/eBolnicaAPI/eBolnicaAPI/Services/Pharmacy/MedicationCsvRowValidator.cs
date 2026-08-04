using eBolnicaAPI.Models.DTOs;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace eBolnicaAPI.Services.Pharmacy
{
    /// <summary>
    /// Maps medication CSV rows to <see cref="MedicationCreateDto"/> and validates using its DataAnnotations rules.
    /// </summary>
    public static class MedicationCsvRowValidator
    {
        public static readonly string[] RequiredHeaders =
        {
            "Name",
            "Generic Name",
            "Category",
            "Manufacturer",
            "Description",
            "Price",
            "Stock Quantity",
            "Minimum Stock Level",
            "Expiry Date",
            "Batch Number",
            "Dosage Form",
            "Strength",
            "Requires Prescription",
            "Active"
        };

        private static readonly Dictionary<string, string> PropertyToCsvHeader = new(StringComparer.Ordinal)
        {
            [nameof(MedicationCreateDto.Name)] = "Name",
            [nameof(MedicationCreateDto.GenericName)] = "Generic Name",
            [nameof(MedicationCreateDto.Category)] = "Category",
            [nameof(MedicationCreateDto.Manufacturer)] = "Manufacturer",
            [nameof(MedicationCreateDto.Description)] = "Description",
            [nameof(MedicationCreateDto.Price)] = "Price",
            [nameof(MedicationCreateDto.StockQuantity)] = "Stock Quantity",
            [nameof(MedicationCreateDto.MinimumStockLevel)] = "Minimum Stock Level",
            [nameof(MedicationCreateDto.ExpiryDate)] = "Expiry Date",
            [nameof(MedicationCreateDto.BatchNumber)] = "Batch Number",
            [nameof(MedicationCreateDto.DosageForm)] = "Dosage Form",
            [nameof(MedicationCreateDto.Strength)] = "Strength",
            [nameof(MedicationCreateDto.RequiresPrescription)] = "Requires Prescription",
            [nameof(MedicationCreateDto.IsActive)] = "Active"
        };

        public static bool TryValidateRow(
            int rowNumber,
            string[] cells,
            IReadOnlyDictionary<string, int> columnIndexes,
            out MedicationCreateDto? dto,
            out List<MedicationImportRowErrorDto> errors)
        {
            dto = null;
            var rowErrors = new List<MedicationImportRowErrorDto>();
            errors = rowErrors;

            string GetRawField(string header)
            {
                if (!columnIndexes.TryGetValue(header, out var index) || index >= cells.Length)
                {
                    return string.Empty;
                }

                return cells[index].Trim();
            }

            void AddError(string field, string? value, string reason)
            {
                rowErrors.Add(new MedicationImportRowErrorDto
                {
                    RowNumber = rowNumber,
                    Field = field,
                    Value = value,
                    Reason = reason
                });
            }

            var name = GetRawField("Name");
            var genericName = GetRawField("Generic Name");
            var category = GetRawField("Category");
            var manufacturer = GetRawField("Manufacturer");
            var description = GetRawField("Description");
            var priceRaw = GetRawField("Price");
            var stockRaw = GetRawField("Stock Quantity");
            var minimumStockRaw = GetRawField("Minimum Stock Level");
            var expiryRaw = GetRawField("Expiry Date");
            var batchNumber = GetRawField("Batch Number");
            var dosageForm = GetRawField("Dosage Form");
            var strength = GetRawField("Strength");
            var requiresPrescriptionRaw = GetRawField("Requires Prescription");
            var activeRaw = GetRawField("Active");

            if (!string.IsNullOrWhiteSpace(priceRaw)
                && !decimal.TryParse(priceRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
            {
                AddError("Price", priceRaw, "Price must be a valid number.");
            }

            if (!string.IsNullOrWhiteSpace(stockRaw) && !TryParseStrictInt(stockRaw, out _))
            {
                AddError("Stock Quantity", stockRaw, "Stock quantity must be a valid integer.");
            }

            if (!string.IsNullOrWhiteSpace(minimumStockRaw) && !TryParseStrictInt(minimumStockRaw, out _))
            {
                AddError("Minimum Stock Level", minimumStockRaw, "Minimum stock level must be a valid integer.");
            }

            if (!string.IsNullOrWhiteSpace(expiryRaw)
                && !DateTime.TryParseExact(
                    expiryRaw,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out _))
            {
                AddError("Expiry Date", expiryRaw, "Expiry date must use YYYY-MM-DD format.");
            }

            if (!string.IsNullOrWhiteSpace(requiresPrescriptionRaw) && !TryParseYesNo(requiresPrescriptionRaw, out _))
            {
                AddError("Requires Prescription", requiresPrescriptionRaw, "Requires Prescription must be Yes or No.");
            }

            if (!string.IsNullOrWhiteSpace(activeRaw) && !TryParseYesNo(activeRaw, out _))
            {
                AddError("Active", activeRaw, "Active must be Yes or No.");
            }

            if (rowErrors.Count > 0)
            {
                return false;
            }

            decimal.TryParse(priceRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var price);
            TryParseStrictInt(stockRaw, out var stockQuantity);
            TryParseStrictInt(minimumStockRaw, out var minimumStockLevel);
            DateTime.TryParseExact(
                expiryRaw,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var expiryDate);
            TryParseYesNo(requiresPrescriptionRaw, out var requiresPrescription);
            TryParseYesNo(activeRaw, out var isActive);

            dto = new MedicationCreateDto
            {
                Name = name,
                GenericName = NullIfEmpty(genericName),
                Category = category,
                Manufacturer = NullIfEmpty(manufacturer),
                Description = NullIfEmpty(description),
                Price = price,
                StockQuantity = stockQuantity,
                MinimumStockLevel = minimumStockLevel,
                ExpiryDate = expiryDate,
                BatchNumber = NullIfEmpty(batchNumber),
                DosageForm = NullIfEmpty(dosageForm),
                Strength = NullIfEmpty(strength),
                RequiresPrescription = requiresPrescription,
                IsActive = isActive
            };

            var validationResults = new List<ValidationResult>();
            Validator.TryValidateObject(
                dto,
                new ValidationContext(dto),
                validationResults,
                validateAllProperties: true);

            foreach (var result in validationResults)
            {
                var memberName = result.MemberNames.FirstOrDefault() ?? string.Empty;
                var field = PropertyToCsvHeader.TryGetValue(memberName, out var csvHeader)
                    ? csvHeader
                    : memberName;
                var rawValue = GetRawField(field);

                errors.Add(new MedicationImportRowErrorDto
                {
                    RowNumber = rowNumber,
                    Field = string.IsNullOrEmpty(field) ? null : field,
                    Value = string.IsNullOrEmpty(rawValue) ? null : rawValue,
                    Reason = result.ErrorMessage ?? "Validation failed."
                });
            }

            return rowErrors.Count == 0;
        }

        private static string? NullIfEmpty(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : value;

        private static bool TryParseStrictInt(string raw, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return false;
            }

            return raw.Trim() == value.ToString(CultureInfo.InvariantCulture);
        }

        private static bool TryParseYesNo(string raw, out bool value)
        {
            value = false;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            if (raw.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }

            if (raw.Equals("no", StringComparison.OrdinalIgnoreCase))
            {
                value = false;
                return true;
            }

            return false;
        }
    }
}
