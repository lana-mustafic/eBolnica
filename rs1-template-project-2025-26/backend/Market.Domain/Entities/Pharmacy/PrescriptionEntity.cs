using Market.Domain.Common;
using Market.Domain.Entities.Clinical;

namespace Market.Domain.Entities.Pharmacy;

public sealed class PrescriptionEntity : BaseEntity
{
    public string PrescriptionNumber { get; set; } = string.Empty;
    public int MedicalReportId { get; set; }
    public MedicalReportEntity MedicalReport { get; set; } = null!;
    public int PatientId { get; set; }
    public PatientEntity Patient { get; set; } = null!;
    public int DoctorId { get; set; }
    public DoctorEntity Doctor { get; set; } = null!;
    public int? PharmacistId { get; set; }
    public PharmacistEntity? Pharmacist { get; set; }
    public string Status { get; set; } = PrescriptionStatuses.Pending;
    public DateTime PrescribedDate { get; set; }
    public DateTime? DispensedDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public ICollection<PrescriptionItemEntity> Items { get; set; } = new List<PrescriptionItemEntity>();
}
