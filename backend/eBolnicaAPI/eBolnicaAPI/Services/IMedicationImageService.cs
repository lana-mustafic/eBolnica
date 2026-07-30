using eBolnicaAPI.Models.DTOs;

namespace eBolnicaAPI.Services
{
    public interface IMedicationImageService
    {
        Task<List<MedicationImageDto>> GetImagesAsync(int medicationId);

        Task<MedicationImageDto> UploadImageAsync(int medicationId, IFormFile file);

        Task SetPrimaryImageAsync(int medicationId, int imageId);

        Task DeleteImageAsync(int medicationId, int imageId);

        Task<List<MedicationImageDto>> ReorderImagesAsync(int medicationId, IReadOnlyList<int> imageIds);

        Task<string?> GetPrimaryImageUrlAsync(int medicationId);
    }
}
