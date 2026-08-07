using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Application.Modules.Pharmacy.Activities;

public static class PharmacyActivityWriter
{
    public static void Record(
        IAppDbContext ctx,
        string eventType,
        string category,
        string severity,
        string message,
        int? actorUserId = null,
        int? prescriptionId = null,
        int? medicationId = null)
    {
        ctx.PharmacyActivities.Add(new PharmacyActivityEntity
        {
            EventType = eventType,
            Category = category,
            Severity = severity,
            Message = message,
            ActorUserId = actorUserId,
            PrescriptionId = prescriptionId,
            MedicationId = medicationId,
            CreatedAtUtc = DateTime.UtcNow
        });
    }
}
