using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MatchFynWebAPI.Data;
using MatchFynWebAPI.DTOs;

namespace MatchFynWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InterestsController : ControllerBase
    {
        private readonly MatchFynDbContext _context;

        public InterestsController(MatchFynDbContext context)
        {
            _context = context;
        }

        // GET: api/Interests
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InterestDto>>> GetInterests()
        {
            var interests = await _context.Interests
                .Where(i => i.IsActive)
                .Select(i => new InterestDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    Description = i.Description,
                    Category = i.Category,
                    IsActive = i.IsActive
                })
                .OrderBy(i => i.Category)
                .ThenBy(i => i.Name)
                .ToListAsync();

            return Ok(interests);
        }

        // GET: api/Interests/categories
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        {
            var categories = await _context.Interests
                .Where(i => i.IsActive && !string.IsNullOrEmpty(i.Category))
                .Select(i => i.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Ok(categories);
        }

        // GET: api/Interests/category/Spor
        [HttpGet("category/{category}")]
        public async Task<ActionResult<IEnumerable<InterestDto>>> GetInterestsByCategory(string category)
        {
            var interests = await _context.Interests
                .Where(i => i.IsActive && i.Category == category)
                .Select(i => new InterestDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    Description = i.Description,
                    Category = i.Category,
                    IsActive = i.IsActive
                })
                .OrderBy(i => i.Name)
                .ToListAsync();

            return Ok(interests);
        }

        // GET: api/Interests/5
        [HttpGet("{id}")]
        public async Task<ActionResult<InterestDto>> GetInterest(int id)
        {
            var interest = await _context.Interests
                .Where(i => i.Id == id && i.IsActive)
                .Select(i => new InterestDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    Description = i.Description,
                    Category = i.Category,
                    IsActive = i.IsActive
                })
                .FirstOrDefaultAsync();

            if (interest == null)
            {
                return NotFound();
            }

            return Ok(interest);
        }
    }
}
