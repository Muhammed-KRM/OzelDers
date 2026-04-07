using Microsoft.AspNetCore.Mvc;
using OzelDers.Business.Interfaces;
using OzelDers.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace OzelDers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BranchesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICacheService _cacheService;

    public BranchesController(AppDbContext context, ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        const string cacheKey = "branches:all";
        var cached = await _cacheService.GetAsync<object>(cacheKey);
        if (cached != null) return Ok(cached);

        var branches = await _context.Branches
            .OrderBy(b => b.DisplayOrder)
            .Select(b => new { b.Id, b.Name, b.Slug, b.IsPopular, b.IconUrl })
            .ToListAsync();

        await _cacheService.SetAsync(cacheKey, branches, TimeSpan.FromHours(1));
        return Ok(branches);
    }
}

