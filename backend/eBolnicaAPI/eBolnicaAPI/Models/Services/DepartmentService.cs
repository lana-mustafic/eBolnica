using eBolnicaAPI.Data.Interfaces;
using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Models.Interfaces;

namespace eBolnicaAPI.Models.Services
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IDepartmentRepository _departmentRepository;

        public DepartmentService(IDepartmentRepository departmentRepository)
        {
            _departmentRepository = departmentRepository;
        }

        public async Task<Department?> GetDepartmentByIdAsync(int id)
        {
            return await _departmentRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Department>> GetAllDepartmentsAsync()
        {
            return await _departmentRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Department>> GetDepartmentsByNameAsync(string name)
        {
            return await _departmentRepository.GetByNameAsync(name);
        }

        public async Task<bool> CreateDepartmentAsync(Department department)
        {
            // Business logic: Department name must be unique
            var existingDepartments = await _departmentRepository.GetAllAsync();
            var existingDepartment = existingDepartments.FirstOrDefault(d =>
                d.Name.Equals(department.Name, StringComparison.OrdinalIgnoreCase));

            if (existingDepartment != null)
            {
                throw new ArgumentException("Department name must be unique.");
            }

            await _departmentRepository.AddAsync(department);
            return await _departmentRepository.SaveChangesAsync();
        }

        public async Task<bool> UpdateDepartmentAsync(Department department)
        {
            _departmentRepository.Update(department);
            return await _departmentRepository.SaveChangesAsync();
        }

        public async Task<bool> DeleteDepartmentAsync(int id)
        {
            var department = await _departmentRepository.GetByIdAsync(id);
            if (department == null)
            {
                return false;
            }

            // Business logic: Cannot delete department with assigned doctors
            if (department.Doctors.Any())
            {
                throw new InvalidOperationException("Cannot delete department with assigned doctors.");
            }

            _departmentRepository.Delete(department);
            return await _departmentRepository.SaveChangesAsync();
        }
    }
}