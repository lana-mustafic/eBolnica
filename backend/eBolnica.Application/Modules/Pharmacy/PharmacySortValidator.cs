using FluentValidation;
using FluentValidation.Results;

namespace eBolnica.Application.Modules.Pharmacy;

public static class PharmacySortValidator
{
    private static readonly HashSet<string> MedicationSortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "name",
        "price",
        "stockquantity",
        "stock",
        "category",
        "expirydate",
        "createdat",
    };

    private static readonly HashSet<string> PrescriptionSortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "prescriptionnumber",
        "status",
        "totalamount",
        "createdat",
        "prescribeddate",
    };

    public static void ValidateMedicationSort(string? sortBy)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            return;

        var column = sortBy.Trim().ToLowerInvariant();
        if (!MedicationSortColumns.Contains(column))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(
                    nameof(sortBy),
                    $"Unknown medication sort column '{sortBy}'. Allowed: name, price, stockQuantity, category, expiryDate, createdAt.")
            });
        }
    }

    public static void ValidatePrescriptionSort(string? sortBy)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            return;

        var column = sortBy.Trim().ToLowerInvariant();
        if (!PrescriptionSortColumns.Contains(column))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(
                    nameof(sortBy),
                    $"Unknown prescription sort column '{sortBy}'. Allowed: prescriptionNumber, status, totalAmount, createdAt, prescribedDate.")
            });
        }
    }
}
