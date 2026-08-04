using Market.Application.Modules.Admin.Users.Queries.List;
using Market.Domain.Entities.Identity;

public sealed class ListUsersQueryHandler(IAppDbContext ctx, IAppCurrentUser currentUser)
    : IRequestHandler<ListUsersQuery, ListUsersQueryDto>
{
    public async Task<ListUsersQueryDto> Handle(ListUsersQuery request, CancellationToken ct)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var page = Math.Max(1, request.Page);

        var query = ctx.Users.AsQueryable();

        if (currentUser.UserId.HasValue)
            query = query.Where(u => u.Id != currentUser.UserId.Value);

        if (!string.IsNullOrWhiteSpace(request.UserType))
            query = query.Where(u => u.UserType == request.UserType);

        var sortBy = request.SortBy.Trim().ToLowerInvariant();
        var asc = request.SortDirection.Trim().ToLowerInvariant() != "desc";

        query = sortBy switch
        {
            "lastname" => asc ? query.OrderBy(u => u.Lastname) : query.OrderByDescending(u => u.Lastname),
            "email" => asc ? query.OrderBy(u => u.Email) : query.OrderByDescending(u => u.Email),
            "usertype" => asc ? query.OrderBy(u => u.UserType) : query.OrderByDescending(u => u.UserType),
            _ => asc ? query.OrderBy(u => u.Firstname) : query.OrderByDescending(u => u.Firstname),
        };

        var totalCount = await query.CountAsync(ct);

        var users = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.Firstname,
                u.Lastname,
                u.Email,
                u.UserType,
                u.LicenseNumber
            })
            .ToListAsync(ct);

        var userIds = users.Select(u => u.Id).ToList();

        var doctors = await ctx.Doctors
            .Where(d => userIds.Contains(d.UserId))
            .Select(d => new { d.Id, d.UserId, d.RegistrationStatus, d.LicenseNumber })
            .ToListAsync(ct);

        var patients = await ctx.Patients
            .Where(p => userIds.Contains(p.UserId))
            .Select(p => new { p.UserId, p.RegistrationStatus })
            .ToListAsync(ct);

        var doctorMap = doctors.ToDictionary(x => x.UserId);
        var patientMap = patients.ToDictionary(x => x.UserId);

        var items = users.Select(u =>
        {
            string? registrationStatus = null;
            string? licenseNumber = u.LicenseNumber;

            if (u.UserType == UserTypes.Doctor && doctorMap.TryGetValue(u.Id, out var doc))
            {
                registrationStatus = doc.RegistrationStatus;
                licenseNumber = doc.LicenseNumber;
            }
            else if (u.UserType == UserTypes.Patient && patientMap.TryGetValue(u.Id, out var pat))
            {
                registrationStatus = pat.RegistrationStatus;
            }

            return new UserOverviewDto
            {
                AppUserId = u.Id,
                FirstName = u.Firstname,
                LastName = u.Lastname,
                Email = u.Email,
                UserType = u.UserType,
                RegistrationStatus = registrationStatus,
                LicenseNumber = licenseNumber,
                DoctorProfileId = u.UserType == UserTypes.Doctor && doctorMap.TryGetValue(u.Id, out var d) ? d.Id : null
            };
        }).ToList();

        return new ListUsersQueryDto
        {
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            SortBy = request.SortBy,
            SortDirection = request.SortDirection,
            Users = items
        };
    }
}
