using eBolnica.Domain.Common;
using eBolnica.Domain.Entities.Identity;

namespace eBolnica.Domain.Entities.Clinical;

public sealed class PharmacistEntity : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int UserId { get; set; }
    public eBolnicaUserEntity User { get; set; } = null!;
    public string LicenseNumber { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateTime? HireDate { get; set; }
}
