using eBolnicaAPI.Models.Entities;

namespace eBolnicaAPI.Models.Interfaces
{
    public interface IPrescriptionService
    {
        Task<Prescription?> GetPrescriptionByIdAsync(int id);
        Task<IEnumerable<Prescription>> GetAllPrescriptionsAsync();
        Task<IEnumerable<Prescription>> GetPrescriptionsByPatientIdAsync(int patientId);
        Task<IEnumerable<Prescription>> GetPrescriptionsByDoctorIdAsync(int doctorId);
        Task<bool> CreatePrescriptionAsync(Prescription prescription);
        Task<bool> UpdatePrescriptionAsync(Prescription prescription);
        Task<bool> DeletePrescriptionAsync(int id);
    }
}