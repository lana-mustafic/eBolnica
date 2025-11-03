using eBolnicaAPI.Models.Entities;

namespace eBolnicaAPI.Data.Interfaces
{
    public interface IAppointmentRepository
    {
        // Get a single appointment by its ID
        Task<Appointment?> GetByIdAsync(int id);

        // Get all appointments
        Task<IEnumerable<Appointment>> GetAllAsync();

        // Add a new appointment to the database
        Task AddAsync(Appointment appointment);

        // Update an existing appointment
        void Update(Appointment appointment);

        // Delete an appointment
        void Delete(Appointment appointment);

        // Save any changes made in the context to the database
        Task<bool> SaveChangesAsync();
    }
}