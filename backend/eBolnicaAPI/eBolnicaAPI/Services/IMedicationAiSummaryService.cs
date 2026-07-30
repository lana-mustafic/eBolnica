using eBolnicaAPI.Models.DTOs;

namespace eBolnicaAPI.Services
{
    public interface IMedicationAiSummaryService
    {
        Task<MedicationAiSummaryDto> GenerateSummaryAsync(
            int medicationId,
            CancellationToken cancellationToken = default);
    }
}
