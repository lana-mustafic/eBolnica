namespace eBolnicaAPI.Services.Pharmacy.MedicationImages
{
    public sealed class ProcessedImageResult : IDisposable
    {
        public ProcessedImageResult(MemoryStream content, string extension, string contentType)
        {
            Content = content;
            Extension = extension;
            ContentType = contentType;
        }

        public MemoryStream Content { get; }

        public string Extension { get; }

        public string ContentType { get; }

        public long Length => Content.Length;

        public void Dispose()
        {
            Content.Dispose();
        }
    }
}
