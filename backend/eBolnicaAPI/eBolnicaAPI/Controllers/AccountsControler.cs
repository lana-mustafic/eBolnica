using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace eBolnicaAPI.Controllers
{
    [Route("api/accounts")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _dbcontext;

        public AccountsController(AppDbContext dbcontext,UserManager<AppUser> userManager)
        {
            _dbcontext = dbcontext;
            _userManager = userManager;
        }

        [HttpPost("patient-registration")]
        public async Task<IActionResult> RegisterPatient([FromBody] UserRegistrationDto userForRegistration)
        {
            if (userForRegistration == null || !ModelState.IsValid)
                return BadRequest(ModelState);


            var user = new AppUser
            {
                Email = userForRegistration.Email,
                UserName = userForRegistration.Email,
                FirstName = userForRegistration.FirstName,
                LastName = userForRegistration.LastName,
                UserType = "Patient"
            };

            var result = await _userManager.CreateAsync(user, userForRegistration.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            var patient = new Patient
            {
                AppUserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
            };

            _dbcontext.Patients.Add(patient);
            await _dbcontext.SaveChangesAsync();

            return Ok(new { message = "Patient registered successfully" });

        }
    }
}
