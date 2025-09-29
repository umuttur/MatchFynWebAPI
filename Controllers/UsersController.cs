using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MatchFynWebAPI.Data;
using MatchFynWebAPI.Models;
using MatchFynWebAPI.DTOs;

namespace MatchFynWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly MatchFynDbContext _context;

        public UsersController(MatchFynDbContext context)
        {
            _context = context;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.UserInterests)
                .ThenInclude(ui => ui.Interest)
                .Where(u => u.IsActive)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    DateOfBirth = u.DateOfBirth,
                    Gender = u.Gender,
                    Bio = u.Bio,
                    ProfileImageUrl = u.ProfileImageUrl,
                    CreatedAt = u.CreatedAt,
                    IsActive = u.IsActive,
                    Interests = u.UserInterests.Select(ui => ui.Interest.Name).ToList()
                })
                .ToListAsync();

            return Ok(users);
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.UserInterests)
                .ThenInclude(ui => ui.Interest)
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

            if (user == null)
            {
                return NotFound();
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                Bio = user.Bio,
                ProfileImageUrl = user.ProfileImageUrl,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive,
                Interests = user.UserInterests.Select(ui => ui.Interest.Name).ToList()
            };

            return Ok(userDto);
        }

        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto createUserDto)
        {
            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == createUserDto.Email))
            {
                return BadRequest("Email already exists");
            }

            var user = new User
            {
                Name = createUserDto.Name,
                Email = createUserDto.Email,
                PhoneNumber = createUserDto.PhoneNumber,
                DateOfBirth = createUserDto.DateOfBirth,
                Gender = createUserDto.Gender,
                Bio = createUserDto.Bio,
                ProfileImageUrl = createUserDto.ProfileImageUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Add user interests
            if (createUserDto.InterestIds.Any())
            {
                var userInterests = createUserDto.InterestIds.Select(interestId => new UserInterest
                {
                    UserId = user.Id,
                    InterestId = interestId
                }).ToList();

                _context.UserInterests.AddRange(userInterests);
                await _context.SaveChangesAsync();
            }

            // Return created user
            var createdUser = await _context.Users
                .Include(u => u.UserInterests)
                .ThenInclude(ui => ui.Interest)
                .FirstAsync(u => u.Id == user.Id);

            var userDto = new UserDto
            {
                Id = createdUser.Id,
                Name = createdUser.Name,
                Email = createdUser.Email,
                PhoneNumber = createdUser.PhoneNumber,
                DateOfBirth = createdUser.DateOfBirth,
                Gender = createdUser.Gender,
                Bio = createdUser.Bio,
                ProfileImageUrl = createdUser.ProfileImageUrl,
                CreatedAt = createdUser.CreatedAt,
                IsActive = createdUser.IsActive,
                Interests = createdUser.UserInterests.Select(ui => ui.Interest.Name).ToList()
            };

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userDto);
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UpdateUserDto updateUserDto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || !user.IsActive)
            {
                return NotFound();
            }

            // Update user properties
            if (!string.IsNullOrEmpty(updateUserDto.Name))
                user.Name = updateUserDto.Name;
            
            if (!string.IsNullOrEmpty(updateUserDto.PhoneNumber))
                user.PhoneNumber = updateUserDto.PhoneNumber;
            
            if (updateUserDto.DateOfBirth.HasValue)
                user.DateOfBirth = updateUserDto.DateOfBirth.Value;
            
            if (!string.IsNullOrEmpty(updateUserDto.Gender))
                user.Gender = updateUserDto.Gender;
            
            if (!string.IsNullOrEmpty(updateUserDto.Bio))
                user.Bio = updateUserDto.Bio;
            
            if (!string.IsNullOrEmpty(updateUserDto.ProfileImageUrl))
                user.ProfileImageUrl = updateUserDto.ProfileImageUrl;

            user.UpdatedAt = DateTime.UtcNow;

            // Update interests if provided
            if (updateUserDto.InterestIds != null)
            {
                // Remove existing interests
                var existingInterests = _context.UserInterests.Where(ui => ui.UserId == id);
                _context.UserInterests.RemoveRange(existingInterests);

                // Add new interests
                var newInterests = updateUserDto.InterestIds.Select(interestId => new UserInterest
                {
                    UserId = id,
                    InterestId = interestId
                }).ToList();

                _context.UserInterests.AddRange(newInterests);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Soft delete
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id && e.IsActive);
        }
    }
}
