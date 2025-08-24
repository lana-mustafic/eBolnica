using eBolnicaAPI.Data.Interfaces;
using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Models.Interfaces;

namespace eBolnicaAPI.Models.Services
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly IPrescriptionRepository _prescriptionRepository;

        public PrescriptionService(IPrescriptionRepository prescriptionRepository)
        {
            _prescriptionRepository = prescriptionRepository;
        }

        public async Task<Prescription?> GetPrescriptionByIdAsync(int id)
        {
            return await _prescriptionRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Prescription>> GetAllPrescriptionsAsync()
        {
            return await _prescriptionRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Prescription>> GetPrescriptionsByPatientIdAsync(int patientId)
        {
            return await _prescriptionRepository.GetByPatientIdAsync(patientId);
        }

        public async Task<IEnumerable<Prescription>> GetPrescriptionsByDoctorIdAsync(int doctorId)
        {
            return await _prescriptionRepository.GetByDoctorIdAsync(doctorId);
        }

        public async Task<bool> CreatePrescriptionAsync(Prescription prescription)
        {
            // Business logic: Expiration date must be after prescription date
            if (prescription.ExpirationDate <= prescription.DatePrescribed)
            {
                throw new ArgumentException("Expiration date must be after prescription date.");
            }

            await _prescriptionRepository.AddAsync(prescription);
            return await _prescriptionRepository.SaveChangesAsync();
        }

        public async Task<bool> UpdatePrescriptionAsync(Prescription prescription)
        {
            _prescriptionRepository.Update(prescription);
            return await _prescriptionRepository.SaveChangesAsync();
        }

        public async Task<bool> DeletePrescriptionAsync(int id)
        {
            var prescription = await _prescriptionRepository.GetByIdAsync(id);
            if (prescription == null)
            {
                return false;
            }

            _prescriptionRepository.Delete(prescription);
            return await _prescriptionRepository.SaveChangesAsync();
        }
    }
}