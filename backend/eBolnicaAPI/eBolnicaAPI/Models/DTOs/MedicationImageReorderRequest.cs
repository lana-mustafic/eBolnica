using System.ComponentModel.DataAnnotations;

namespace eBolnicaAPI.Models.DTOs
{
    public class MedicationImageReorderRequest
    {
        [Required]
        [MinLength(1)]
        public List<int> ImageIds { get; set; } = new();
    }
}
