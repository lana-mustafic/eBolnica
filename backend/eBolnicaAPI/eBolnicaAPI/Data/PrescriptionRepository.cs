using eBolnicaAPI.Data.Interfaces;
using eBolnicaAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace eBolnicaAPI.Data
{
    public class PrescriptionRepository : IPrescriptionRepository
    {
        private readonly AppDbContext _context;

        public PrescriptionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Prescription?> GetByIdAsync(int id)
        {
            return await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Prescription>> GetAllAsync()
        {
            return await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .ToListAsync();
        }

        public async Task<IEnumerable<Prescription>> GetByPatientIdAsync(int patientId)
        {
            return await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .Where(p => p.PatientId == patientId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Prescription>> GetByDoctorIdAsync(int doctorId)
        {
            return await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .Where(p => p.DoctorId == doctorId)
                .ToListAsync();
        }

        public async Task AddAsync(Prescription prescription)
        {
            await _context.Prescriptions.AddAsync(prescription);
        }

        public void Update(Prescription prescription)
        {
            _context.Prescriptions.Update(prescription);
        }

        public void Delete(Prescription prescription)
        {
            _context.Prescriptions.Remove(prescription);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}