using Microsoft.AspNetCore.Mvc;
using OzelDers.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace OzelDers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CitiesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CitiesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var cities = await _context.Cities
            .Include(c => c.Districts)
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Slug,
                c.PlateCode,
                Districts = c.Districts.OrderBy(d => d.Name).Select(d => new { d.Id, d.Name, d.Slug })
            })
            .ToListAsync();
        return Ok(cities);
    }
}
