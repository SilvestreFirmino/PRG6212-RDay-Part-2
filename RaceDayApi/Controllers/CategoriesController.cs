using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[Route("api")]
public class CategoriesController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }

    [HttpGet("events/{eventId}/categories")]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        List<Category> categories = await _db.Categories
            .Where(c => c.EventId == eventId && c.IsActive)
            .ToListAsync();

        return Ok(categories);
    }

    [HttpPost("events/{eventId}/categories")]
    public async Task<IActionResult> CreateCategory(int eventId, Category category)
    {
        if (!IsOrganiser())
        {
            return Unauthorized("Only organisers can create categories.");
        }

        RaceEvent? raceEvent = await _db.Events.FindAsync(eventId);

        if (raceEvent == null || raceEvent.OrganiserId != CurrentUserId)
        {
            return NotFound("Your event was not found.");
        }

        if (category.MaximumAge.HasValue && category.MinimumAge > category.MaximumAge)
        {
            return BadRequest("Maximum age must be greater than minimum age.");
        }

        category.CategoryId = 0;
        category.EventId = eventId;
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return Ok("Category created successfully.");
    }

    [HttpPut("categories/{categoryId}")]
    public async Task<IActionResult> UpdateCategory(int categoryId, Category newDetails)
    {
        if (!IsOrganiser())
        {
            return Unauthorized("Only organisers can update categories.");
        }

        Category? category = await _db.Categories.Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

        if (category == null || category.Event == null || category.Event.OrganiserId != CurrentUserId)
        {
            return NotFound("Your category was not found.");
        }

        category.Name = newDetails.Name;
        category.Description = newDetails.Description;
        category.DistanceKm = newDetails.DistanceKm;
        category.EntryFee = newDetails.EntryFee;
        category.Capacity = newDetails.Capacity;
        category.MinimumAge = newDetails.MinimumAge;
        category.MaximumAge = newDetails.MaximumAge;
        category.CategoryStartTime = newDetails.CategoryStartTime;
        category.IsActive = newDetails.IsActive;

        await _db.SaveChangesAsync();
        return Ok("Category updated successfully.");
    }

    [HttpDelete("categories/{categoryId}")]
    public async Task<IActionResult> DeleteCategory(int categoryId)
    {
        if (!IsOrganiser())
        {
            return Unauthorized("Only organisers can delete categories.");
        }

        Category? category = await _db.Categories.Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

        if (category == null || category.Event == null || category.Event.OrganiserId != CurrentUserId)
        {
            return NotFound("Your category was not found.");
        }

        bool hasEnrolments = await _db.EventEnrollments.AnyAsync(e => e.CategoryId == categoryId);
        if (hasEnrolments)
        {
            return BadRequest("A category with enrolments cannot be deleted.");
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return Ok("Category deleted successfully.");
    }
}
