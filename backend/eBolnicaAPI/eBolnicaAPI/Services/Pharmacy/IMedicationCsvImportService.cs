using eBolnicaAPI.Models.DTOs;
using Microsoft.AspNetCore.Http;

namespace eBolnicaAPI.Services.Pharmacy
{
    public interface IMedicationCsvImportService
    {
        int MaxFileSizeBytes { get; }

        int MaxImportRows { get; }

        /// <summary>
        /// Validates and imports medications from a CSV upload in a single batch transaction.
        /// Returns a file-level error message when the upload cannot be processed (400).
        /// </summary>
        Task<(string? FileError, MedicationImportResultDto? Result)> ImportAsync(
            IFormFile? file,
            CancellationToken cancellationToken = default);
    }
}
