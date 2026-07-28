namespace eBolnicaAPI.Services.Pharmacy.MedicationImages
{
    public interface IMedicationImageThumbnailGenerator
    {
        Task<MemoryStream> GenerateAsync(Stream content, CancellationToken cancellationToken = default);
    }
}
