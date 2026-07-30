namespace eBolnicaAPI.Services.Pharmacy.MedicationAiSummary
{
    public interface IMedicationAiSummaryClient
    {
        Task<string> GenerateSummaryJsonAsync(
            string systemPrompt,
            string userPrompt,
            CancellationToken cancellationToken = default);
    }
}
