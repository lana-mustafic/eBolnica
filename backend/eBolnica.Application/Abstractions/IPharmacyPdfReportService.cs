using eBolnica.Application.Modules.Pharmacy.Analytics;
using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Application.Abstractions;

public interface IPharmacyPdfReportService
{
    byte[] GenerateInventoryPdf(
        InventoryPdfSummary summary,
        Func<int, int, IReadOnlyList<MedicationEntity>> fetchBatch);

    byte[] GeneratePrescriptionsPdf(
        PrescriptionsPdfSummary summary,
        Func<int, int, IReadOnlyList<PrescriptionEntity>> fetchBatch);
}
