using Market.Application.Modules.Doctor.Patients.Queries.ListDoctorPatients;

public sealed class ListDoctorPatientsQueryHandler(IAppDbContext ctx, IAppCurrentUser currentUser)
    : IRequestHandler<ListDoctorPatientsQuery, ListDoctorPatientsQueryDto>
{
    public async Task<ListDoctorPatientsQueryDto> Handle(ListDoctorPatientsQuery request, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            throw new MarketBusinessRuleException("auth.not_authenticated", "User is not authenticated.");

        var doctor = await ctx.Doctors
            .FirstOrDefaultAsync(d => d.UserId == currentUser.UserId.Value, ct);

        if (doctor is null)
            throw new MarketNotFoundException("Doctor profile not found.");

        var query = ctx.Patients
            .Include(p => p.MedicalRecord)
            .Where(p => p.DoctorId == doctor.Id)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.FirstName))
            query = query.Where(p => p.FirstName.ToLower().Contains(request.FirstName.ToLower()));

        if (!string.IsNullOrWhiteSpace(request.LastName))
            query = query.Where(p => p.LastName.ToLower().Contains(request.LastName.ToLower()));

        if (!string.IsNullOrWhiteSpace(request.Gender))
            query = query.Where(p => p.Gender == request.Gender);

        if (!string.IsNullOrWhiteSpace(request.BloodType))
            query = query.Where(p => p.BloodType == request.BloodType);

        if (request.BirthYear.HasValue)
            query = query.Where(p => p.DateOfBirth.HasValue && p.DateOfBirth.Value.Year == request.BirthYear.Value);

        var totalCount = await query.CountAsync(ct);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var patients = await query
            .OrderBy(p => p.LastName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new DoctorAssignedPatientDto
            {
                Id = p.Id,
                DoctorId = p.DoctorId!.Value,
                FirstName = p.FirstName,
                LastName = p.LastName,
                DateOfBirth = p.DateOfBirth,
                Gender = p.Gender,
                PhoneNumber = p.PhoneNumber,
                Address = p.Address,
                BloodType = p.BloodType,
                RecordNumber = p.MedicalRecord != null ? p.MedicalRecord.RecordNumber : string.Empty
            })
            .ToListAsync(ct);

        return new ListDoctorPatientsQueryDto
        {
            Items = patients,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}
