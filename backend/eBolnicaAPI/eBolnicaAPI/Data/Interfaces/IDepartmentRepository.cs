using eBolnicaAPI.Models.Entities;

namespace eBolnicaAPI.Data.Interfaces
{
    public interface IDepartmentRepository
    {
        Task<Department?> GetByIdAsync(int id);
        Task<IEnumerable<Department>> GetAllAsync();
        Task<IEnumerable<Department>> GetByNameAsync(string name);
        Task AddAsync(Department department);
        void Update(Department department);
        void Delete(Department department);
        Task<bool> SaveChangesAsync();
    }
}