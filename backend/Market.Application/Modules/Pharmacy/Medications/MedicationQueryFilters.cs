using Market.Application.Modules.Pharmacy.Medications.Queries.ListMedications;
using Market.Domain.Entities.Pharmacy;

namespace Market.Application.Modules.Pharmacy.Medications;

internal static class MedicationQueryFilters
{
    public static IQueryable<MedicationEntity> Apply(
        IQueryable<MedicationEntity> query,
        string? search,
        string? category,
        bool? isActive,
        bool includeInactive,
        string? stockStatus)
    {
        query = query.Where(m => !m.IsDeleted);

        if (isActive.HasValue)
            query = query.Where(m => m.IsActive == isActive.Value);
        else if (!includeInactive)
            query = query.Where(m => m.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(m =>
                m.Name.ToLower().Contains(term) ||
                (m.GenericName != null && m.GenericName.ToLower().Contains(term)) ||
                (m.Manufacturer != null && m.Manufacturer.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(m => m.Category == category);

        if (!string.IsNullOrWhiteSpace(stockStatus))
        {
            var status = stockStatus.Trim().ToLowerInvariant();
            query = status switch
            {
                "out of stock" => query.Where(m => m.StockQuantity == 0),
                "low stock" => query.Where(m => m.StockQuantity > 0 && m.StockQuantity < m.MinimumStockLevel),
                "normal stock" or "instock" => query.Where(m => m.StockQuantity >= m.MinimumStockLevel),
                _ => query
            };
        }

        return query;
    }

    public static IQueryable<MedicationEntity> ApplySorting(
        IQueryable<MedicationEntity> query, string? sortBy, string? sortOrder)
    {
        var desc = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        var field = sortBy?.Trim().ToLowerInvariant() ?? "createdat";

        return field switch
        {
            "name" => desc ? query.OrderByDescending(m => m.Name) : query.OrderBy(m => m.Name),
            "price" => desc ? query.OrderByDescending(m => m.Price) : query.OrderBy(m => m.Price),
            "stockquantity" or "stock" => desc
                ? query.OrderByDescending(m => m.StockQuantity)
                : query.OrderBy(m => m.StockQuantity),
            "category" => desc ? query.OrderByDescending(m => m.Category) : query.OrderBy(m => m.Category),
            "expirydate" => desc
                ? query.OrderByDescending(m => m.ExpiryDate)
                : query.OrderBy(m => m.ExpiryDate),
            _ => desc
                ? query.OrderByDescending(m => m.CreatedAtUtc)
                : query.OrderBy(m => m.CreatedAtUtc)
        };
    }
}
