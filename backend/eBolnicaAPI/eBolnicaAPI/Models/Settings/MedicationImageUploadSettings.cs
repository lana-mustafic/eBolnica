namespace eBolnicaAPI.Models.Settings
{
    /// <summary>
    /// Configuration for medication image upload pipeline.
    /// </summary>
    public class MedicationImageUploadSettings
    {
        public const string SectionName = "MedicationImageUpload";

        public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;

        public int MaxWidth { get; set; } = 1920;

        public int MaxHeight { get; set; } = 1920;

        public int JpegQuality { get; set; } = 85;

        public bool StripMetadata { get; set; } = true;

        public string UploadSubDirectory { get; set; } = "medications";

        public int ThumbnailWidth { get; set; } = 128;

        public int ThumbnailHeight { get; set; } = 128;

        public int ThumbnailJpegQuality { get; set; } = 80;

        public string StaticFilesPhysicalFolder { get; set; } = "Uploads";

        public string StaticFilesRequestPath { get; set; } = "/uploads";

        public int StaticFileCacheMaxAgeSeconds { get; set; } = 86400;

        public bool StaticFileCacheImmutable { get; set; } = true;
    }
}
