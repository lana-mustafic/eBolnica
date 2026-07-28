namespace eBolnicaAPI.Services.Pharmacy.MedicationImages
{
    public interface IMedicationImageOptimizer
    {
        Task<ProcessedImageResult> OptimizeAsync(Stream content, string extension, CancellationToken cancellationToken = default);
    }
}
