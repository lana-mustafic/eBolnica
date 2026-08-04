using System.ComponentModel.DataAnnotations;

namespace eBolnicaAPI.Models.DTOs
{
    /// <summary>
    /// Data transfer object for dispensing a prescription
    /// </summary>
    public class PrescriptionDispenseDto
    {
        [Required(ErrorMessage = "Pharmacist ID is required")]
        public int PharmacistId { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? DispensedDate { get; set; }
    }
}
