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
    public IActionResult GetCategories(int eventId)
    {
        List<Category> categories = _db.Categories
            .Where(c => c.EventId == eventId && c.IsActive)
            .ToList();

        return Ok(categories);
    }

    [HttpPost("events/{eventId}/categories")]
    public IActionResult CreateCategory(int eventId, Category category)
    {
        if (!IsOrganiser())
        {
            return Unauthorized("Only organisers can create categories.");
        }

        RaceEvent? raceEvent = _db.Events.Find(eventId);

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
        _db.SaveChanges();

        return Ok("Category created successfully.");
    }

    [HttpPut("categories/{categoryId}")]
    public IActionResult UpdateCategory(int categoryId, Category newDetails)
    {
        if (!IsOrganiser())
        {
            return Unauthorized("Only organisers can update categories.");
        }

        Category? category = _db.Categories.Include(c => c.Event)
            .FirstOrDefault(c => c.CategoryId == categoryId);

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

        _db.SaveChanges();
        return Ok("Category updated successfully.");
    }

    [HttpDelete("categories/{categoryId}")]
    public IActionResult DeleteCategory(int categoryId)
    {
        if (!IsOrganiser())
        {
            return Unauthorized("Only organisers can delete categories.");
        }

        Category? category = _db.Categories.Include(c => c.Event)
            .FirstOrDefault(c => c.CategoryId == categoryId);

        if (category == null || category.Event == null || category.Event.OrganiserId != CurrentUserId)
        {
            return NotFound("Your category was not found.");
        }

        bool hasEnrolments = _db.EventEnrollments.Any(e => e.CategoryId == categoryId);
        if (hasEnrolments)
        {
            return BadRequest("A category with enrolments cannot be deleted.");
        }

        _db.Categories.Remove(category);
        _db.SaveChanges();

        return Ok("Category deleted successfully.");
    }
}
