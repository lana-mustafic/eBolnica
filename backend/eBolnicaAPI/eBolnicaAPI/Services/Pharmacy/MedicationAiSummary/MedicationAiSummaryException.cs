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
        public MedicationAiSummaryUnavailableException(
            string userMessage,
            string? logMessage = null,
            int statusCode = StatusCodes.Status503ServiceUnavailable)
            : base(logMessage ?? userMessage)
        {
            UserMessage = userMessage;
            StatusCode = statusCode;
        }

        public MedicationAiSummaryUnavailableException(
            string userMessage,
            Exception innerException,
            string? logMessage = null,
            int statusCode = StatusCodes.Status503ServiceUnavailable)
            : base(logMessage ?? userMessage, innerException)
        {
            UserMessage = userMessage;
            StatusCode = statusCode;
        }

        public string UserMessage { get; }

        public int StatusCode { get; }

        public static MedicationAiSummaryUnavailableException Timeout(Exception? innerException = null) =>
            innerException == null
                ? new MedicationAiSummaryUnavailableException(
                    MedicationAiSummaryErrorMessages.Timeout,
                    "AI summary LLM request timed out.",
                    StatusCodes.Status504GatewayTimeout)
                : new MedicationAiSummaryUnavailableException(
                    MedicationAiSummaryErrorMessages.Timeout,
                    innerException,
                    "AI summary LLM request timed out.",
                    StatusCodes.Status504GatewayTimeout);

        public static MedicationAiSummaryUnavailableException ServiceUnavailable(
            string? logMessage = null,
            Exception? innerException = null) =>
            innerException == null
                ? new MedicationAiSummaryUnavailableException(
                    MedicationAiSummaryErrorMessages.Unavailable,
                    logMessage ?? MedicationAiSummaryErrorMessages.Unavailable)
                : new MedicationAiSummaryUnavailableException(
                    MedicationAiSummaryErrorMessages.Unavailable,
                    innerException,
                    logMessage ?? MedicationAiSummaryErrorMessages.Unavailable);

        public static MedicationAiSummaryUnavailableException NotConfigured(string? logMessage = null) =>
            new(
                MedicationAiSummaryErrorMessages.NotConfigured,
                logMessage ?? MedicationAiSummaryErrorMessages.NotConfigured);

        public static MedicationAiSummaryUnavailableException InvalidProviderResponse(string? logMessage = null) =>
            new(
                MedicationAiSummaryErrorMessages.Unavailable,
                logMessage ?? "AI summary provider returned an invalid response.");
    }
}
