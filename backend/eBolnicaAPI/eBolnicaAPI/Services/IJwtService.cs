using eBolnicaAPI.Data;
using eBolnicaAPI.Models.Entities;

namespace eBolnicaAPI.Services
{
    public interface IJwtService
    {
        string GenerateToken(AppUser user);
    }
}
