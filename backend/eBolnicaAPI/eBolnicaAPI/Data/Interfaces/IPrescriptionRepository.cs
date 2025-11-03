using eBolnicaAPI.Models.Entities;

namespace eBolnicaAPI.Data.Interfaces
{
    public interface IPrescriptionRepository
    {
        Task<Prescription?> GetByIdAsync(int id);
        Task<IEnumerable<Prescription>> GetAllAsync();
        Task<IEnumerable<Prescription>> GetByPatientIdAsync(int patientId);
        Task<IEnumerable<Prescription>> GetByDoctorIdAsync(int doctorId);
        Task AddAsync(Prescription prescription);
        void Update(Prescription prescription);
        void Delete(Prescription prescription);
        Task<bool> SaveChangesAsync();
    }
}