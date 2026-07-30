namespace eBolnicaAPI.Services.Pharmacy.MedicationAiSummary
{
    public class MedicationAiSummaryException : Exception
    {
        public MedicationAiSummaryException(string message) : base(message)
        {
        }

        public MedicationAiSummaryException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class MedicationAiSummaryUnavailableException : MedicationAiSummaryException
    {
        public MedicationAiSummaryUnavailableException(string message) : base(message)
        {
        }

        public MedicationAiSummaryUnavailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
