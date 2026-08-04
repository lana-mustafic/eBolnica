using System.ComponentModel.DataAnnotations;

namespace eBolnicaAPI.Models.Entities
{
    /// <summary>
    /// Represents an image associated with a medication
    /// </summary>
    public class MedicationImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MedicationId { get; set; }

        public Medication Medication { get; set; } = null!;

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string RelativeUrl { get; set; } = string.Empty;

        public string? ThumbnailRelativeUrl { get; set; }

        public bool IsPrimary { get; set; }

        public int SortOrder { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public long? FileSizeBytes { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }
    }
}
