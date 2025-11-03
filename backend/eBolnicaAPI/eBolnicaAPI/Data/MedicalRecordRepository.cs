using eBolnicaAPI.Data.Interfaces;
using eBolnicaAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace eBolnicaAPI.Data
{
    public class MedicalRecordRepository : IMedicalRecordRepository
    {
        private readonly AppDbContext _context;

        public MedicalRecordRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<MedicalRecord?> GetByIdAsync(int id)
        {
            return await _context.MedicalRecords
                .Include(m => m.Patient)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<IEnumerable<MedicalRecord>> GetAllAsync()
        {
            return await _context.MedicalRecords
                .Include(m => m.Patient)
                .ToListAsync();
        }

        public async Task<IEnumerable<MedicalRecord>> GetByPatientIdAsync(int patientId)
        {
            return await _context.MedicalRecords
                .Include(m => m.Patient)
                .Where(m => m.PatientId == patientId)
                .ToListAsync();
        }

        public async Task AddAsync(MedicalRecord medicalRecord)
        {
            await _context.MedicalRecords.AddAsync(medicalRecord);
        }

        public void Update(MedicalRecord medicalRecord)
        {
            _context.MedicalRecords.Update(medicalRecord);
        }

        public void Delete(MedicalRecord medicalRecord)
        {
            _context.MedicalRecords.Remove(medicalRecord);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}