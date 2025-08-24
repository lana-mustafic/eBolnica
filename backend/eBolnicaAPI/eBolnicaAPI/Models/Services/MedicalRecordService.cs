using eBolnicaAPI.Data.Interfaces;
using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Models.Interfaces;

namespace eBolnicaAPI.Models.Services
{
    public class MedicalRecordService : IMedicalRecordService
    {
        private readonly IMedicalRecordRepository _medicalRecordRepository;

        public MedicalRecordService(IMedicalRecordRepository medicalRecordRepository)
        {
            _medicalRecordRepository = medicalRecordRepository;
        }

        public async Task<MedicalRecord?> GetMedicalRecordByIdAsync(int id)
        {
            return await _medicalRecordRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<MedicalRecord>> GetAllMedicalRecordsAsync()
        {
            return await _medicalRecordRepository.GetAllAsync();
        }

        public async Task<IEnumerable<MedicalRecord>> GetMedicalRecordsByPatientIdAsync(int patientId)
        {
            return await _medicalRecordRepository.GetByPatientIdAsync(patientId);
        }

        public async Task<bool> CreateMedicalRecordAsync(MedicalRecord medicalRecord)
        {
            // Business logic: Record date cannot be in the future
            if (medicalRecord.RecordDate > DateTime.UtcNow)
            {
                throw new ArgumentException("Record date cannot be in the future.");
            }

            await _medicalRecordRepository.AddAsync(medicalRecord);
            return await _medicalRecordRepository.SaveChangesAsync();
        }

        public async Task<bool> UpdateMedicalRecordAsync(MedicalRecord medicalRecord)
        {
            _medicalRecordRepository.Update(medicalRecord);
            return await _medicalRecordRepository.SaveChangesAsync();
        }

        public async Task<bool> DeleteMedicalRecordAsync(int id)
        {
            var medicalRecord = await _medicalRecordRepository.GetByIdAsync(id);
            if (medicalRecord == null)
            {
                return false;
            }

            _medicalRecordRepository.Delete(medicalRecord);
            return await _medicalRecordRepository.SaveChangesAsync();
        }
    }
}