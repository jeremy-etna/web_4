using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using quest_web.DAL;
using quest_web.DTO;
using quest_web.Models;
using quest_web.Utils;

namespace quest_web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class UserController : Controller
    {
        private readonly ILogger<UserController> _logger;
        private readonly APIDbContext _context;
        private readonly JwtTokenUtil _jwt;

        public UserController(ILogger<UserController> logger, APIDbContext context, JwtTokenUtil jwt)
        {
            _context = context;
            _logger = logger;
            _jwt = jwt;
        }

        [Authorize]
        [HttpGet("/user")]
        public async Task<ObjectResult> GetUser()
        {
            return Ok(await _context.User.ToListAsync());
        }

        [Authorize]
        [HttpGet("/user/{id:int}/addresses")]
        public async Task<ObjectResult> GetUserAddresses(int id)
        {
            var user = await _context.User.FirstOrDefaultAsync(u => u.ID == id);

            if (user == null)
            {
                return BadRequest(new { error = "User ID doesn't exist: " + id });
            }

            var addresses = await _context.Address.Where(a => a.User == id).ToListAsync();

            return Ok(addresses);
        }

        [Authorize]
        [HttpGet("/user/{id:int}")]
        public async Task<ObjectResult> GetUserById(int id)
        {
            var user =  await _context.User.FirstOrDefaultAsync(u => u.ID == id);

            if (user == null)
            {
                return BadRequest(new { error = "User ID doesn't exist: " + id});
            }

            return Ok(user);
        }

        [Authorize]
        [HttpPut("/user/{id:int}")]
        public async Task<ObjectResult> UpdateUserUsername(int id, [FromBody] UserNameUpdateParams user)
        {
            var accessToken = await HttpContext.GetTokenAsync("access_token");
            var username = _jwt.GetUsernameFromToken(accessToken);
            var currentUser = _context.User.First(u => u.Username == username);

            if (!currentUser.Role.Equals(UserRole.ROLE_ADMIN) &&
                currentUser.ID != id)
            {
                return Unauthorized(new { error = "You don't have permission to perform this request."});
            }

            var userData = await _context.User.FirstOrDefaultAsync(u => u.ID == id);
            if (userData == null)
                return BadRequest(new { error = "User ID does not exist."});

            if (currentUser.Role.Equals(UserRole.ROLE_ADMIN) &&
                user.Role != null)
            {
                userData.Role = (UserRole)user.Role;
            }

            userData.Username = user.Username ?? userData.Username;

            _context.Update(userData);
            await _context.SaveChangesAsync();

            return Ok(userData);
        }

        [Authorize]
        [HttpDelete("/user/{id:int}")]
        public async Task<ObjectResult> DeleteUserById(int id)
        {
            var accessToken = await HttpContext.GetTokenAsync("access_token");
            var username = _jwt.GetUsernameFromToken(accessToken);
            var currentUser = await _context.User.FirstOrDefaultAsync(u => u.Username == username);

            var user = await _context.User.FirstOrDefaultAsync(u => u.ID == id);
            if (user == null)
                return BadRequest(new { error = "User ID does not exist."});

            if (!currentUser.Role.Equals(UserRole.ROLE_ADMIN) &&
                currentUser.ID != user.ID)
            {
                return Unauthorized(new { error = "Permission error"});
            }

            _context.Remove(user);
            await _context.SaveChangesAsync();

            return Ok("Success");
        }


    }
}