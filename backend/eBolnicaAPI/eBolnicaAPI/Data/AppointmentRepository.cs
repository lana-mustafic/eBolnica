using eBolnicaAPI.Data.Interfaces;
using eBolnicaAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace eBolnicaAPI.Data
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly AppDbContext _context;

        // Constructor: Dependency Injection gives us the database context
        public AppointmentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Appointment?> GetByIdAsync(int id)
        {
            // Get appointment and include related Patient and Doctor data
            return await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<Appointment>> GetAllAsync()
        {
            // Get all appointments and include related data
            return await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .ToListAsync();
        }

        public async Task AddAsync(Appointment appointment)
        {
            // Add a new appointment to the context
            await _context.Appointments.AddAsync(appointment);
        }

        public void Update(Appointment appointment)
        {
            // Mark an appointment as modified
            _context.Appointments.Update(appointment);
        }

        public void Delete(Appointment appointment)
        {
            // Mark an appointment for deletion
            _context.Appointments.Remove(appointment);
        }

        public async Task<bool> SaveChangesAsync()
        {
            // Save all changes in the context to the database
            // Returns true if at least one row was affected
            return (await _context.SaveChangesAsync()) > 0;
        }
    }
}