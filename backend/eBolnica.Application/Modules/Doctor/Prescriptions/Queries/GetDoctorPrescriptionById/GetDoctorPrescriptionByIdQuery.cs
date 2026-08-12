namespace eBolnica.Application.Modules.Doctor.Prescriptions.Queries.GetDoctorPrescriptionById;

using eBolnica.Application.Modules.Pharmacy.Prescriptions;

public sealed class GetDoctorPrescriptionByIdQuery : IRequest<PrescriptionDto>
{
    public int Id { get; init; }
}

public sealed class GetDoctorPrescriptionByIdQueryValidator : AbstractValidator<GetDoctorPrescriptionByIdQuery>
{
    public GetDoctorPrescriptionByIdQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

public sealed class GetDoctorPrescriptionByIdQueryHandler(IAppDbContext ctx, IAppCurrentUser currentUser)
    : IRequestHandler<GetDoctorPrescriptionByIdQuery, PrescriptionDto>
{
    public async Task<PrescriptionDto> Handle(GetDoctorPrescriptionByIdQuery request, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            throw new eBolnicaBusinessRuleException("auth.not_authenticated", "User is not authenticated.");

        var doctor = await ctx.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == currentUser.UserId.Value, ct);

        if (doctor is null)
            throw new eBolnicaNotFoundException("Doctor profile not found.");

        var prescription = await ctx.Prescriptions
            .AsNoTracking()
            .Where(p => p.Id == request.Id && p.DoctorId == doctor.Id)
            .WithDetails()
            .FirstOrDefaultAsync(ct);

        if (prescription is null)
            throw new eBolnicaNotFoundException("Prescription not found.");

        return PrescriptionMapping.MapToDto(prescription);
    }
}
