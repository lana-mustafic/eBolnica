using Market.Domain.Entities.Pharmacy;

namespace Market.Application.Abstractions;

public interface IPharmacyPdfReportService
{
    byte[] GenerateInventoryPdf(IReadOnlyList<MedicationEntity> medications);
    byte[] GeneratePrescriptionsPdf(IReadOnlyList<PrescriptionEntity> prescriptions);
}
