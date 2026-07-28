using Microsoft.AspNetCore.Http;

namespace eBolnicaAPI.Services.Pharmacy.MedicationImages
{
    public interface IMedicationImageFileValidator
    {
        void ValidateUpload(IFormFile file);

        string SanitizeOriginalFileName(string fileName);
    }
}
