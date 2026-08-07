using eBolnica.Application.Modules.Pharmacy.Prescriptions;

namespace eBolnica.Application.Abstractions;

public interface IPrescriptionCreationService
{
    Task<PrescriptionDto> CreateAsync(PrescriptionCreationRequest request, CancellationToken ct = default);
}
