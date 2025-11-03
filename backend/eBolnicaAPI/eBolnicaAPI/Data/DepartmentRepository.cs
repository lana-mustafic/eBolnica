using eBolnicaAPI.Data.Interfaces;
using eBolnicaAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace eBolnicaAPI.Data
{
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly AppDbContext _context;

        public DepartmentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Department?> GetByIdAsync(int id)
        {
            return await _context.Departments
                .Include(d => d.Doctors)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<IEnumerable<Department>> GetAllAsync()
        {
            return await _context.Departments
                .Include(d => d.Doctors)
                .ToListAsync();
        }

        public async Task<IEnumerable<Department>> GetByNameAsync(string name)
        {
            return await _context.Departments
                .Include(d => d.Doctors)
                .Where(d => d.Name.Contains(name))
                .ToListAsync();
        }

        public async Task AddAsync(Department department)
        {
            await _context.Departments.AddAsync(department);
        }

        public void Update(Department department)
        {
            _context.Departments.Update(department);
        }

        public void Delete(Department department)
        {
            _context.Departments.Remove(department);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}