using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OOTD_API.Models;

namespace OOTD_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly Ootdv1Context db;

        public UsersController(Ootdv1Context context)
        {
            db = context;
        }

        /// <summary>
        /// Get all users
        /// </summary>
        /// <returns>A list of users</returns>
        [HttpGet]
        public IActionResult Get()
        {
            var users = db.Users.Select(x => new
            {
                x.Uid,
                x.Username
            }).ToList<object>();

            return Ok(users);
        }
    }
}
