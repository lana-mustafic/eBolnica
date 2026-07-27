namespace eBolnicaAPI.Services.Pharmacy.MedicationImages
{
    public sealed class ProcessedImageResult : IDisposable
    {
        public ProcessedImageResult(
            MemoryStream content,
            string extension,
            string contentType,
            int width,
            int height)
        {
            Content = content;
            Extension = extension;
            ContentType = contentType;
            Width = width;
            Height = height;
        }

        public MemoryStream Content { get; }

        public string Extension { get; }

        public string ContentType { get; }

        public int Width { get; }

        public int Height { get; }

        public long Length => Content.Length;

        public void Dispose()
        {
            Content.Dispose();
        }
    }
}
