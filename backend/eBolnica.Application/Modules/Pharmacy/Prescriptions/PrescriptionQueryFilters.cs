using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Application.Modules.Pharmacy.Prescriptions;

public static class PrescriptionQueryFilters
{
    public static IQueryable<PrescriptionEntity> Apply(
        IQueryable<PrescriptionEntity> query,
        string? status,
        string? search)
    {
        if (!string.IsNullOrWhiteSpace(status) &&
            !string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
        {
            var normalizedStatus = status.Trim();
            query = query.Where(p => p.Status.ToLower() == normalizedStatus.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(p =>
                p.PrescriptionNumber.ToLower().Contains(term) ||
                p.Patient.FirstName.ToLower().Contains(term) ||
                p.Patient.LastName.ToLower().Contains(term) ||
                p.Doctor.FirstName.ToLower().Contains(term) ||
                p.Doctor.LastName.ToLower().Contains(term) ||
                p.Items.Any(i => i.Medication.Name.ToLower().Contains(term)));
        }

        return query;
    }

    public static IQueryable<PrescriptionEntity> ApplySorting(
        IQueryable<PrescriptionEntity> query,
        string? sortBy,
        string? sortOrder)
    {
        var desc = !string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);
        var column = (sortBy ?? "prescribedDate").ToLowerInvariant();

        return column switch
        {
            "prescriptionnumber" => desc
                ? query.OrderByDescending(p => p.PrescriptionNumber)
                : query.OrderBy(p => p.PrescriptionNumber),
            "status" => desc
                ? query.OrderByDescending(p => p.Status)
                : query.OrderBy(p => p.Status),
            "totalamount" => desc
                ? query.OrderByDescending(p => p.TotalAmount)
                : query.OrderBy(p => p.TotalAmount),
            "createdat" => desc
                ? query.OrderByDescending(p => p.CreatedAtUtc)
                : query.OrderBy(p => p.CreatedAtUtc),
            _ => desc
                ? query.OrderByDescending(p => p.PrescribedDate)
                : query.OrderBy(p => p.PrescribedDate)
        };
    }
}
