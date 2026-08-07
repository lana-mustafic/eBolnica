using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Application.Modules.Pharmacy.Prescriptions;

public static class PrescriptionQueryFilters
{
    public static IQueryable<PrescriptionEntity> Apply(
        IQueryable<PrescriptionEntity> query,
        string? status,
        string? search,
        DateTime? prescribedFrom = null,
        DateTime? prescribedTo = null,
        string? patientSearch = null,
        string? doctorSearch = null)
    {
        if (!string.IsNullOrWhiteSpace(status) &&
            !string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => p.Status == status.Trim());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var normalizedMedicationTerm = MedicationEntity.NormalizeName(term);
            query = query.Where(p =>
                p.PrescriptionNumber.Contains(term) ||
                p.Items.Any(i => i.Medication.NormalizedName.Contains(normalizedMedicationTerm)));
        }

        if (!string.IsNullOrWhiteSpace(patientSearch))
        {
            var term = patientSearch.Trim();
            query = query.Where(p =>
                p.Patient.FirstName.Contains(term) ||
                p.Patient.LastName.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(doctorSearch))
        {
            var term = doctorSearch.Trim();
            query = query.Where(p =>
                p.Doctor.FirstName.Contains(term) ||
                p.Doctor.LastName.Contains(term));
        }

        if (prescribedFrom.HasValue)
            query = query.Where(p => p.PrescribedDate >= prescribedFrom.Value);

        if (prescribedTo.HasValue)
            query = query.Where(p => p.PrescribedDate <= prescribedTo.Value);

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
