namespace eBolnica.Domain.Entities.Pharmacy;

public static class PharmacyActivityEventTypes
{
    public const string PrescriptionCreated = "prescription.created";
    public const string PrescriptionDispensed = "prescription.dispensed";
    public const string PrescriptionCancelled = "prescription.cancelled";
    public const string MedicationCreated = "medication.created";
    public const string MedicationDeleted = "medication.deleted";
    public const string StockAdjusted = "inventory.stock_adjusted";
    public const string MedicationsImported = "medication.imported";
}
