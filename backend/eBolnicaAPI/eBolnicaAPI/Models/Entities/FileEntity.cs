namespace eBolnicaAPI.Models.Entities
{
    public class FileEntity
    {
        public int Id { get; set; }

        public string FileName { get; set; }

        public string FilePath { get; set; }

        public string ContentType { get; set; }

        public long FileSize { get; set; }

        public DateTime UploadedAt { get; set; }

        public int PatientId { get; set; }
        public virtual Patient Patient { get; set; }
    }
}
