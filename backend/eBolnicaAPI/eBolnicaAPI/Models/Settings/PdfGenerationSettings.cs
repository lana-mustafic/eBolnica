namespace eBolnicaAPI.Models.Settings
{
    /// <summary>
    /// Configuration settings for PDF generation
    /// </summary>
    public class PdfGenerationSettings
    {
        /// <summary>
        /// Default page size (e.g., "A4", "Letter")
        /// </summary>
        public string DefaultPageSize { get; set; } = "A4";

        /// <summary>
        /// Margin size in millimeters
        /// </summary>
        public int MarginMillimeters { get; set; } = 20;

        /// <summary>
        /// Header height in points
        /// </summary>
        public int HeaderHeight { get; set; } = 50;

        /// <summary>
        /// Footer height in points
        /// </summary>
        public int FooterHeight { get; set; } = 30;

        /// <summary>
        /// Font family name
        /// </summary>
        public string FontFamily { get; set; } = "Helvetica";

        /// <summary>
        /// Primary color for headings and accents (hex format)
        /// </summary>
        public string PrimaryColor { get; set; } = "#2196F3";

        /// <summary>
        /// Secondary color for text and borders (hex format)
        /// </summary>
        public string SecondaryColor { get; set; } = "#757575";

        /// <summary>
        /// Background color for table headers (hex format)
        /// </summary>
        public string TableHeaderBackground { get; set; } = "#F5F5F5";

        /// <summary>
        /// Maximum rows per page for tables
        /// </summary>
        public int MaxRowsPerPage { get; set; } = 40;

        /// <summary>
        /// Whether to include page numbers in footer
        /// </summary>
        public bool IncludePageNumbers { get; set; } = true;

        /// <summary>
        /// Whether to include generation timestamp in header/footer
        /// </summary>
        public bool IncludeGenerationTimestamp { get; set; } = true;

        /// <summary>
        /// Compression level (0-9, higher = more compression)
        /// </summary>
        public int CompressionLevel { get; set; } = 5;

        /// <summary>
        /// Image quality for embedded images (0-100)
        /// </summary>
        public int ImageQuality { get; set; } = 90;

        /// <summary>
        /// Whether to cache generated PDFs
        /// </summary>
        public bool CacheGeneratedPdfs { get; set; } = true;

        /// <summary>
        /// Cache duration in minutes
        /// </summary>
        public int CacheDurationMinutes { get; set; } = 60;
    }
}
