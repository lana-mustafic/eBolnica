namespace eBolnicaAPI.Models.DTOs
{
    /// <summary>
    /// Structured AI-generated medication summary returned to the pharmacy UI.
    /// </summary>
    public class MedicationAiSummaryDto
    {
        public string Overview { get; set; } = string.Empty;

        public string UsageNotes { get; set; } = string.Empty;

        public string StockExpiryAlert { get; set; } = string.Empty;

        public string PrescriptionRequirement { get; set; } = string.Empty;
    }
}
