namespace eBolnicaAPI.Models.DTOs
{
    public class PatientFilterParams
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Gender { get; set; }
        public string? BloodType { get; set; }
        public int? BirthYear { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
