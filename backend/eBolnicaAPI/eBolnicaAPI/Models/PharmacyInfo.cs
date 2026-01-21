namespace eBolnicaAPI.Models
{
    /// <summary>
    /// Pharmacy information for PDF reports header
    /// </summary>
    public class PharmacyInfo
    {
        /// <summary>
        /// Pharmacy name
        /// </summary>
        public string Name { get; set; } = "eBolnica Pharmacy";

        /// <summary>
        /// Pharmacy address
        /// </summary>
        public string Address { get; set; } = "123 Medical Street, City, State 12345";

        /// <summary>
        /// Pharmacy phone number
        /// </summary>
        public string Phone { get; set; } = "+1 (555) 123-4567";

        /// <summary>
        /// Pharmacy email
        /// </summary>
        public string Email { get; set; } = "pharmacy@ebolnica.com";

        /// <summary>
        /// Pharmacy license number
        /// </summary>
        public string? LicenseNumber { get; set; }
    }
}
