using eBolnica.Application.Modules.Pharmacy.Prescriptions;

namespace eBolnica.Application.Modules.Pharmacy.Prescriptions.Queries.GetPrescriptionById;

public sealed class GetPrescriptionByIdQueryHandler(IAppDbContext ctx)
    : IRequestHandler<GetPrescriptionByIdQuery, PrescriptionDto>
{
    public async Task<PrescriptionDto> Handle(GetPrescriptionByIdQuery request, CancellationToken ct)
    {
        var prescription = await ctx.Prescriptions
            .AsNoTracking()
            .WithDetails()
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct);

        if (prescription is null)
            throw new eBolnicaNotFoundException("Prescription not found.");

        return PrescriptionMapping.MapToDto(prescription);
    }
}
