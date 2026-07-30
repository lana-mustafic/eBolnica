using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Settings;
using eBolnicaAPI.Services.Pharmacy.MedicationAiSummary;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace eBolnicaAPI.Services
{
    public class MedicationAiSummaryService : IMedicationAiSummaryService
    {
        private readonly AppDbContext _context;
        private readonly IMedicationAiSummaryClient _client;
        private readonly MedicationAiSummarySettings _settings;
        private readonly ILogger<MedicationAiSummaryService> _logger;

        public MedicationAiSummaryService(
            AppDbContext context,
            IMedicationAiSummaryClient client,
            IOptions<MedicationAiSummarySettings> settings,
            ILogger<MedicationAiSummaryService> logger)
        {
            _context = context;
            _client = client;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<MedicationAiSummaryDto> GenerateSummaryAsync(
            int medicationId,
            CancellationToken cancellationToken = default)
        {
            var medication = await _context.Medications
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == medicationId, cancellationToken);

            if (medication == null)
            {
                throw new KeyNotFoundException("Medication not found");
            }

            EnsureServiceConfigured();

            var userPrompt = MedicationAiSummaryPromptBuilder.BuildUserPrompt(medication);

            try
            {
                var json = await _client.GenerateSummaryJsonAsync(
                    MedicationAiSummaryPromptBuilder.SystemPrompt,
                    userPrompt,
                    cancellationToken);

                return MedicationAiSummaryResponseParser.Parse(json);
            }
            catch (MedicationAiSummaryUnavailableException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate AI summary for medication {MedicationId}", medicationId);
                throw MedicationAiSummaryUnavailableException.ServiceUnavailable(
                    "Unexpected AI summary generation failure.");
            }
        }

        private void EnsureServiceConfigured()
        {
            MedicationAiSummaryLlmRequestBuilder.EnsureConfigured(_settings);
        }
    }
}
