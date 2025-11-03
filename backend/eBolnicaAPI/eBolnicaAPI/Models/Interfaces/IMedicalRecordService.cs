using eBolnicaAPI.Models.Entities;

namespace eBolnicaAPI.Models.Interfaces
{
    public interface IMedicalRecordService
    {
        Task<MedicalRecord?> GetMedicalRecordByIdAsync(int id);
        Task<IEnumerable<MedicalRecord>> GetAllMedicalRecordsAsync();
        Task<IEnumerable<MedicalRecord>> GetMedicalRecordsByPatientIdAsync(int patientId);
        Task<bool> CreateMedicalRecordAsync(MedicalRecord medicalRecord);
        Task<bool> UpdateMedicalRecordAsync(MedicalRecord medicalRecord);
        Task<bool> DeleteMedicalRecordAsync(int id);
    }
}