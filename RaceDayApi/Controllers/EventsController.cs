using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.DTOs;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[Route("api/events")]
public class EventsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    public EventsController(RaceDayDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RaceEvent>>> GetAll([FromQuery] string? search, [FromQuery] string? eventType, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _db.Events.Include(e => e.Categories).Where(e => e.Status == "Published" || e.Status == "Completed").AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(e => e.Name.Contains(search));
        if (!string.IsNullOrWhiteSpace(eventType)) query = query.Where(e => e.EventType == eventType);
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        return Ok(await query.OrderBy(e => e.StartDateTime).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync());
    }

    [HttpGet("{eventId:int}")]
    public async Task<ActionResult<RaceEvent>> Get(int eventId)
    {
        var item = await _db.Events.Include(e => e.Categories).SingleOrDefaultAsync(e => e.EventId == eventId && (e.Status == "Published" || e.Status == "Completed"));
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IEnumerable<RaceEvent>>> Mine()
    {
        if (!IsOrganiser()) return Unauthorized("Only organisers can view their events.");
        return Ok(await _db.Events.Where(e => e.OrganiserId == CurrentUserId).OrderByDescending(e => e.CreatedAtUtc).ToListAsync());
    }

    [HttpPost]
    public async Task<ActionResult<RaceEvent>> Create(EventRequest request)
    {
        if (!IsOrganiser()) return Unauthorized("Only organisers can create events.");
        if (request.EndDateTime <= request.StartDateTime || request.RegistrationCloseUtc <= request.RegistrationOpenUtc) return BadRequest("Dates are invalid.");
        var item = new RaceEvent { OrganiserId = CurrentUserId, Name = request.Name.Trim(), Description = request.Description.Trim(), EventType = request.EventType.Trim(), StartDateTime = request.StartDateTime, EndDateTime = request.EndDateTime, TimeZoneId = request.TimeZoneId.Trim(), VenueName = request.VenueName.Trim(), AddressLine1 = request.AddressLine1.Trim(), City = request.City.Trim(), Province = request.Province.Trim(), PostalCode = request.PostalCode?.Trim(), RegistrationOpenUtc = request.RegistrationOpenUtc, RegistrationCloseUtc = request.RegistrationCloseUtc };
        _db.Events.Add(item); await _db.SaveChangesAsync(); return CreatedAtAction(nameof(GetOwned), new { eventId = item.EventId }, item);
    }

    [HttpGet("owned/{eventId:int}")]
    public async Task<ActionResult<RaceEvent>> GetOwned(int eventId)
    {
        if (!IsOrganiser()) return Unauthorized("Only organisers can view this event.");
        var item = await _db.Events.Include(e => e.Categories).SingleOrDefaultAsync(e => e.EventId == eventId && e.OrganiserId == CurrentUserId);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPut("{eventId:int}")]
    public async Task<IActionResult> Update(int eventId, EventRequest request)
    {
        if (!IsOrganiser()) return Unauthorized("Only organisers can update events.");
        var item = await Owned(eventId); if (item is null) return NotFound();
        if (item.Status is "Completed" or "Cancelled" || request.EndDateTime <= request.StartDateTime) return Conflict("This event cannot be edited.");
        item.Name = request.Name.Trim(); item.Description = request.Description.Trim(); item.EventType = request.EventType.Trim(); item.StartDateTime = request.StartDateTime; item.EndDateTime = request.EndDateTime; item.TimeZoneId = request.TimeZoneId.Trim(); item.VenueName = request.VenueName.Trim(); item.AddressLine1 = request.AddressLine1.Trim(); item.City = request.City.Trim(); item.Province = request.Province.Trim(); item.PostalCode = request.PostalCode?.Trim(); item.RegistrationOpenUtc = request.RegistrationOpenUtc; item.RegistrationCloseUtc = request.RegistrationCloseUtc; item.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(); return NoContent();
    }

    [HttpPatch("{eventId:int}/status")]
    public async Task<IActionResult> SetStatus(int eventId, EventStatusRequest request)
    {
        if (!IsOrganiser()) return Unauthorized("Only organisers can change event status.");
        var item = await _db.Events.Include(e => e.Categories).SingleOrDefaultAsync(e => e.EventId == eventId && e.OrganiserId == CurrentUserId); if (item is null) return NotFound();
        if (request.Status == "Published" && !item.Categories.Any(c => c.IsActive)) return Conflict("A published event needs an active category.");
        if (request.Status is not ("Draft" or "Published" or "Completed" or "Cancelled")) return BadRequest("Invalid status.");
        item.Status = request.Status; item.UpdatedAtUtc = DateTime.UtcNow; await _db.SaveChangesAsync(); return NoContent();
    }

    [HttpDelete("{eventId:int}")]
    public async Task<IActionResult> Delete(int eventId)
    {
        if (!IsOrganiser()) return Unauthorized("Only organisers can delete events.");
        var item = await Owned(eventId); if (item is null) return NotFound();
        if (item.Status != "Draft" || await _db.EventEnrollments.AnyAsync(e => e.EventId == eventId)) return Conflict("Only an empty draft can be deleted.");
        _db.Events.Remove(item); await _db.SaveChangesAsync(); return NoContent();
    }

    private Task<RaceEvent?> Owned(int eventId) => _db.Events.SingleOrDefaultAsync(e => e.EventId == eventId && e.OrganiserId == CurrentUserId);
}
