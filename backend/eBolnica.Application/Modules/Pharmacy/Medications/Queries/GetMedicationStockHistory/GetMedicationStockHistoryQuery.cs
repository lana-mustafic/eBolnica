namespace eBolnica.Application.Modules.Pharmacy.Medications.Queries.GetMedicationStockHistory;

using eBolnica.Application.Modules.Pharmacy.Medications;

public sealed class GetMedicationStockHistoryQuery : IRequest<IReadOnlyList<MedicationStockHistoryDto>>
{
    public int MedicationId { get; init; }
    public int Limit { get; init; } = 50;
}
