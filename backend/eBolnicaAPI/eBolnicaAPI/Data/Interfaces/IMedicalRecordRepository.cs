using eBolnicaAPI.Models.Entities;

namespace eBolnicaAPI.Data.Interfaces
{
    public interface IMedicalRecordRepository
    {
        Task<MedicalRecord?> GetByIdAsync(int id);
        Task<IEnumerable<MedicalRecord>> GetAllAsync();
        Task<IEnumerable<MedicalRecord>> GetByPatientIdAsync(int patientId); // New specific method
        Task AddAsync(MedicalRecord medicalRecord);
        void Update(MedicalRecord medicalRecord);
        void Delete(MedicalRecord medicalRecord);
        Task<bool> SaveChangesAsync();
    }
}