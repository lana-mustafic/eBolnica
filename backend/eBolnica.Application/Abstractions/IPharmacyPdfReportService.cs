using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Application.Abstractions;

public interface IPharmacyPdfReportService
{
    byte[] GenerateInventoryPdf(IReadOnlyList<MedicationEntity> medications);
    byte[] GeneratePrescriptionsPdf(IReadOnlyList<PrescriptionEntity> prescriptions);
}
