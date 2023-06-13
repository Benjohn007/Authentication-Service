using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AdminController : ControllerBase
    {
        [HttpGet("Employee")]
        public IEnumerable<string> GetRoles()
        {
            return new List<string> { "Ahmed", "Ali", "Dada" };
        }
    }
}
