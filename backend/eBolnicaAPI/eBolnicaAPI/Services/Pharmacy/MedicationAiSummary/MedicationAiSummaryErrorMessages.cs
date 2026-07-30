namespace eBolnicaAPI.Services.Pharmacy.MedicationAiSummary
{
    internal static class MedicationAiSummaryErrorMessages
    {
        public const string Unavailable =
            "The AI summary service is temporarily unavailable. Please try again later.";

        public const string Timeout =
            "The AI summary request timed out. Please try again later.";

        public const string NotConfigured =
            "The AI summary service is not configured.";

        public const string MedicationNotFound =
            "Medication not found";

        public const string Fallback =
            "Unable to generate an AI summary right now. The rest of this page is still available.";
    }
}
