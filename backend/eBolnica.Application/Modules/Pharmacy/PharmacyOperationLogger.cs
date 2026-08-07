using Microsoft.Extensions.Logging;

namespace eBolnica.Application.Modules.Pharmacy;

internal static class PharmacyOperationLogger
{
    public static void PrescriptionCreated(
        ILogger logger,
        int prescriptionId,
        string prescriptionNumber,
        int patientId,
        int? userId) =>
        logger.LogInformation(
            "Prescription created. PrescriptionId={PrescriptionId} PrescriptionNumber={PrescriptionNumber} PatientId={PatientId} UserId={UserId}",
            prescriptionId,
            prescriptionNumber,
            patientId,
            userId);

    public static void PrescriptionDispensed(
        ILogger logger,
        int prescriptionId,
        string prescriptionNumber,
        int pharmacistId) =>
        logger.LogInformation(
            "Prescription dispensed. PrescriptionId={PrescriptionId} PrescriptionNumber={PrescriptionNumber} PharmacistId={PharmacistId}",
            prescriptionId,
            prescriptionNumber,
            pharmacistId);

    public static void MedicationCreated(ILogger logger, int medicationId, string name) =>
        logger.LogInformation(
            "Medication created. MedicationId={MedicationId} Name={MedicationName}",
            medicationId,
            name);

    public static void MedicationDeleted(ILogger logger, int medicationId, string name, int? userId) =>
        logger.LogInformation(
            "Medication deleted. MedicationId={MedicationId} Name={MedicationName} UserId={UserId}",
            medicationId,
            name,
            userId);

    public static void MedicationImageUploaded(
        ILogger logger,
        int medicationId,
        int imageId,
        string fileName) =>
        logger.LogInformation(
            "Medication image uploaded. MedicationId={MedicationId} ImageId={ImageId} FileName={FileName}",
            medicationId,
            imageId,
            fileName);

    public static void MedicationImageDeleted(
        ILogger logger,
        int medicationId,
        int imageId,
        bool wasPrimary) =>
        logger.LogInformation(
            "Medication image deleted. MedicationId={MedicationId} ImageId={ImageId} WasPrimary={WasPrimary}",
            medicationId,
            imageId,
            wasPrimary);

    public static void MedicationPrimaryImageSet(ILogger logger, int medicationId, int imageId) =>
        logger.LogInformation(
            "Medication primary image set. MedicationId={MedicationId} ImageId={ImageId}",
            medicationId,
            imageId);
}
