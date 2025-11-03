using eBolnicaAPI.Models.Entities;

namespace eBolnicaAPI.Models.Interfaces
{
    public interface IDepartmentService
    {
        Task<Department?> GetDepartmentByIdAsync(int id);
        Task<IEnumerable<Department>> GetAllDepartmentsAsync();
        Task<IEnumerable<Department>> GetDepartmentsByNameAsync(string name);
        Task<bool> CreateDepartmentAsync(Department department);
        Task<bool> UpdateDepartmentAsync(Department department);
        Task<bool> DeleteDepartmentAsync(int id);
    }
}