namespace eBolnicaAPI.Models.DTOs
{
    /// <summary>
    /// Data transfer object for medication image information
    /// </summary>
    public class MedicationImageDto
    {
        public int Id { get; set; }

        public int MedicationId { get; set; }

        public string FileName { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        public string? ThumbnailUrl { get; set; }

        public bool IsPrimary { get; set; }

        public int SortOrder { get; set; }

        public DateTime UploadedAt { get; set; }
    }
}
