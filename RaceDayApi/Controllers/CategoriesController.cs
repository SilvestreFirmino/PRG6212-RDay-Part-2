using System.ComponentModel.DataAnnotations;
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
        var race = _db.Events.Find(eventId);
        if (race == null || (race.Status != "Published" && race.Status != "Completed" && race.OrganiserId != CurrentUserId)) return NotFound();
        List<Category> categories = _db.Categories
            .Where(c => c.EventId == eventId && (c.IsActive || race.OrganiserId == CurrentUserId))
            .ToList();

        return Ok(categories);
    }

    [HttpPost("events/{eventId}/categories")]
    public IActionResult CreateCategory(
        int eventId,
        [FromForm, Required, MaxLength(100)] string name,
        [FromForm] decimal distanceKm,
        [FromForm] decimal entryFee,
        [FromForm] int capacity,
        [FromForm, MaxLength(500)] string? description = null,
        [FromForm] int? minimumAge = null,
        [FromForm] int? maximumAge = null,
        [FromForm] TimeSpan? categoryStartTime = null,
        [FromForm] bool isActive = true)
    {
        Category category = new Category
        {
            Name = name,
            Description = description,
            DistanceKm = distanceKm,
            EntryFee = entryFee,
            Capacity = capacity,
            MinimumAge = minimumAge,
            MaximumAge = maximumAge,
            CategoryStartTime = categoryStartTime,
            IsActive = isActive
        };

        if (!IsOrganiser())
        {
            return Unauthorized("Only organisers can create categories.");
        }

        RaceEvent? raceEvent = _db.Events.Find(eventId);

        if (raceEvent == null || raceEvent.OrganiserId != CurrentUserId)
        {
            return NotFound("Your event was not found.");
        }

        if (distanceKm <= 0 || entryFee < 0 || capacity < 1 || minimumAge < 0 || maximumAge < 0)
            return BadRequest("Distance and capacity must be positive; fees and ages cannot be negative.");
        if (_db.Categories.Any(c => c.EventId == eventId && c.Name == name))
            return BadRequest("A category with that name already exists.");
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
    public IActionResult UpdateCategory(
        int categoryId,
        [FromForm, Required, MaxLength(100)] string name,
        [FromForm] decimal distanceKm,
        [FromForm] decimal entryFee,
        [FromForm] int capacity,
        [FromForm, MaxLength(500)] string? description = null,
        [FromForm] int? minimumAge = null,
        [FromForm] int? maximumAge = null,
        [FromForm] TimeSpan? categoryStartTime = null,
        [FromForm] bool isActive = true)
    {
        Category newDetails = new Category
        {
            Name = name,
            Description = description,
            DistanceKm = distanceKm,
            EntryFee = entryFee,
            Capacity = capacity,
            MinimumAge = minimumAge,
            MaximumAge = maximumAge,
            CategoryStartTime = categoryStartTime,
            IsActive = isActive
        };

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

        if (distanceKm <= 0 || entryFee < 0 || capacity < 1 || minimumAge < 0 || maximumAge < 0 || minimumAge > maximumAge)
            return BadRequest("Check category distance, fee, capacity and age limits.");
        if (_db.Categories.Any(c => c.EventId == category.EventId && c.Name == name && c.CategoryId != categoryId))
            return BadRequest("A category with that name already exists.");
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
