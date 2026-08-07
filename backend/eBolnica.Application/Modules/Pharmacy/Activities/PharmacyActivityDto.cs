namespace eBolnica.Application.Modules.Pharmacy.Activities;

public sealed class PharmacyActivityDto
{
    public int Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}
