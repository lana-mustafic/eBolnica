using eBolnicaAPI.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace eBolnicaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("not-found")]
        public IActionResult GetNotFound()
        {
            throw new NotFoundException("This resource was not found");
        }

        [HttpGet("bad-request")]
        public IActionResult GetBadRequest()
        {
            throw new BadRequestException("This is a bad request");
        }

        [HttpGet("server-error")]
        public IActionResult GetServerError()
        {
            throw new Exception("This is a server error");
        }
    }
}