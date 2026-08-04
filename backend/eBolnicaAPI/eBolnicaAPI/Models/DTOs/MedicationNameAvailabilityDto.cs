namespace eBolnicaAPI.Models.DTOs
{
    /// <summary>
    /// Response for GET /medications/check-name
    /// </summary>
    public class MedicationNameAvailabilityDto
    {
        public bool IsAvailable { get; set; }
    }
}
