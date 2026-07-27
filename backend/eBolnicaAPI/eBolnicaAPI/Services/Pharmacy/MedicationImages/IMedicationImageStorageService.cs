namespace eBolnicaAPI.Services.Pharmacy.MedicationImages
{
    public sealed class StoredMedicationImageResult
    {
        public required string RelativeUrl { get; init; }

        public required string StoredFileName { get; init; }
    }

    public interface IMedicationImageStorageService
    {
        Task<StoredMedicationImageResult> SaveAsync(int medicationId, Stream content, string extension);

        void Delete(string relativeUrl);
    }
}
