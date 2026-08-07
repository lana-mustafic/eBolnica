namespace eBolnica.Application.Abstractions;

public interface IPrescriptionNumberGenerator
{
    Task<string> ReserveNextAsync(int year, CancellationToken ct = default);
}
