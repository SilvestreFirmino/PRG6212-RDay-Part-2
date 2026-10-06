using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.DTOs;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[Route("api")]
public class CategoriesController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    public CategoriesController(RaceDayDbContext db) => _db = db;

    [AllowAnonymous]
    [HttpGet("events/{eventId:int}/categories")]
    public async Task<ActionResult<IEnumerable<Category>>> List(int eventId) => Ok(await _db.Categories.Where(c => c.EventId == eventId && c.IsActive).ToListAsync());

    [AllowAnonymous]
    [HttpGet("categories/{categoryId:int}")]
    public async Task<ActionResult<Category>> Get(int categoryId)
    {
        var category = await _db.Categories.SingleOrDefaultAsync(c => c.CategoryId == categoryId && c.IsActive);
        return category is null ? NotFound() : Ok(category);
    }

    [Authorize(Roles = Roles.Organiser)]
    [HttpPost("events/{eventId:int}/categories")]
    public async Task<ActionResult<Category>> Create(int eventId, CategoryRequest request)
    {
        if (!await OwnsEvent(eventId)) return NotFound();
        if (request.MaximumAge.HasValue && request.MinimumAge > request.MaximumAge) return BadRequest("Maximum age must be greater than minimum age.");
        var category = new Category { EventId = eventId, Name = request.Name.Trim(), Description = request.Description?.Trim(), DistanceKm = request.DistanceKm, EntryFee = request.EntryFee, Capacity = request.Capacity, MinimumAge = request.MinimumAge, MaximumAge = request.MaximumAge, CategoryStartTime = request.CategoryStartTime, IsActive = request.IsActive };
        _db.Categories.Add(category); await _db.SaveChangesAsync(); return CreatedAtAction(nameof(Get), new { categoryId = category.CategoryId }, category);
    }

    [Authorize(Roles = Roles.Organiser)]
    [HttpPut("categories/{categoryId:int}")]
    public async Task<IActionResult> Update(int categoryId, CategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).SingleOrDefaultAsync(c => c.CategoryId == categoryId && c.Event!.OrganiserId == CurrentUserId); if (category is null) return NotFound();
        int enrollmentCount = await _db.EventEnrollments.CountAsync(e => e.CategoryId == categoryId && e.Status == "Confirmed");
        if (request.Capacity < enrollmentCount) return Conflict("Capacity cannot be lower than confirmed enrolments.");
        category.Name = request.Name.Trim(); category.Description = request.Description?.Trim(); category.DistanceKm = request.DistanceKm; category.EntryFee = request.EntryFee; category.Capacity = request.Capacity; category.MinimumAge = request.MinimumAge; category.MaximumAge = request.MaximumAge; category.CategoryStartTime = request.CategoryStartTime; category.IsActive = request.IsActive;
        await _db.SaveChangesAsync(); return NoContent();
    }

    [Authorize(Roles = Roles.Organiser)]
    [HttpDelete("categories/{categoryId:int}")]
    public async Task<IActionResult> Delete(int categoryId)
    {
        var category = await _db.Categories.Include(c => c.Event).SingleOrDefaultAsync(c => c.CategoryId == categoryId && c.Event!.OrganiserId == CurrentUserId); if (category is null) return NotFound();
        if (await _db.EventEnrollments.AnyAsync(e => e.CategoryId == categoryId)) return Conflict("Categories with enrolments cannot be deleted.");
        _db.Categories.Remove(category); await _db.SaveChangesAsync(); return NoContent();
    }

    private Task<bool> OwnsEvent(int eventId) => _db.Events.AnyAsync(e => e.EventId == eventId && e.OrganiserId == CurrentUserId);
}
