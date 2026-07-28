namespace eBolnicaAPI.Services.Pharmacy.MedicationImages
{
    public interface IMedicationImageVirusScanner
    {
        Task ScanAsync(Stream content, CancellationToken cancellationToken = default);
    }
}
