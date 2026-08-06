namespace eBolnica.Application.Modules.Pharmacy.Prescriptions.Queries.SearchPrescriptionPatients;

public sealed class SearchPrescriptionPatientsQueryHandler(IAppDbContext ctx, IAppCurrentUser currentUser)
    : IRequestHandler<SearchPrescriptionPatientsQuery, IReadOnlyList<PrescriptionFormPatientDto>>
{
    public async Task<IReadOnlyList<PrescriptionFormPatientDto>> Handle(
        SearchPrescriptionPatientsQuery request,
        CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            throw new eBolnicaBusinessRuleException("auth.not_authenticated", "User is not authenticated.");

        var pharmacist = await ctx.Pharmacists
            .AnyAsync(p => p.UserId == currentUser.UserId.Value, ct);

        if (!pharmacist)
            throw new eBolnicaNotFoundException("Pharmacist profile not found.");

        var limit = Math.Clamp(request.Limit, 1, 50);
        var query = ctx.Patients
            .AsNoTracking()
            .Where(p => ctx.MedicalReports.Any(r => r.MedicalRecord!.PatientId == p.Id));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(p =>
                p.FirstName.Contains(term) ||
                p.LastName.Contains(term) ||
                (p.FirstName + " " + p.LastName).Contains(term));
        }

        return await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Take(limit)
            .Select(p => new PrescriptionFormPatientDto
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                PhoneNumber = p.PhoneNumber
            })
            .ToListAsync(ct);
    }
}
