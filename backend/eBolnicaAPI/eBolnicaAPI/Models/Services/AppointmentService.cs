using eBolnicaAPI.Data.Interfaces;
using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Models.Interfaces;

namespace eBolnicaAPI.Models.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _appointmentRepository;

        // Constructor: Dependency Injection gives us the repository
        public AppointmentService(IAppointmentRepository appointmentRepository)
        {
            _appointmentRepository = appointmentRepository;
        }

        public async Task<Appointment?> GetAppointmentByIdAsync(int id)
        {
            // Business logic: Get appointment by ID
            return await _appointmentRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Appointment>> GetAllAppointmentsAsync()
        {
            // Business logic: Get all appointments
            return await _appointmentRepository.GetAllAsync();
        }

        public async Task<bool> CreateAppointmentAsync(Appointment appointment)
        {
            // Business logic: Check for future date (example rule)
            if (appointment.DateTime <= DateTime.Now)
            {
                throw new ArgumentException("Appointment date must be in the future.");
            }

            // Add the appointment using the repository
            await _appointmentRepository.AddAsync(appointment);

            // Save changes and return success status
            return await _appointmentRepository.SaveChangesAsync();
        }

        public async Task<bool> UpdateAppointmentAsync(Appointment appointment)
        {
            // Business logic could be added here (e.g., check if status change is valid)

            // Update the appointment using the repository
            _appointmentRepository.Update(appointment);

            // Save changes and return success status
            return await _appointmentRepository.SaveChangesAsync();
        }

        public async Task<bool> DeleteAppointmentAsync(int id)
        {
            // Business logic: Get the appointment first
            var appointment = await _appointmentRepository.GetByIdAsync(id);
            if (appointment == null)
            {
                return false; // Appointment not found
            }

            // Business logic: Check if cancellation is allowed (example rule)
            if (appointment.Status == "Completed")
            {
                throw new InvalidOperationException("Cannot delete a completed appointment.");
            }

            // Delete the appointment using the repository
            _appointmentRepository.Delete(appointment);

            // Save changes and return success status
            return await _appointmentRepository.SaveChangesAsync();
        }
    }
}