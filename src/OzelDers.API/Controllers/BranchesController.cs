using Microsoft.AspNetCore.Mvc;
using OzelDers.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace OzelDers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BranchesController : ControllerBase
{
    private readonly AppDbContext _context;

    public BranchesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var branches = await _context.Branches
            .OrderBy(b => b.DisplayOrder)
            .Select(b => new { b.Id, b.Name, b.Slug, b.IsPopular, b.IconUrl })
            .ToListAsync();
        return Ok(branches);
    }
}
