using eBolnica.Application.Modules.Pharmacy.Analytics;
using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Application.Abstractions;

public interface IPharmacyPdfReportService
{
    byte[] GenerateInventoryPdf(
        InventoryPdfSummary summary,
        IReadOnlyList<MedicationEntity> items);

    byte[] GeneratePrescriptionsPdf(
        PrescriptionsPdfSummary summary,
        IReadOnlyList<PrescriptionEntity> items);
}
