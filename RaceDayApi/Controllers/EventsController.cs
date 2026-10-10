using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[Route("api/events")]
public class EventsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db)
    {
        _db = db;
    }

    // Anyone can see published and completed events.
    [HttpGet]
    public async Task<IActionResult> GetEvents()
    {
        List<RaceEvent> events = await _db.Events
            .Where(e => e.Status == "Published" || e.Status == "Completed")
            .OrderBy(e => e.StartDateTime)
            .ToListAsync();

        return Ok(events);
    }

    [HttpGet("{eventId}")]
    public async Task<IActionResult> GetEvent(int eventId)
    {
        RaceEvent? raceEvent = await _db.Events.FindAsync(eventId);

        if (raceEvent == null)
        {
            return NotFound("Event not found.");
        }

        return Ok(raceEvent);
    }

    // An organiser can see all their events, including drafts.
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyEvents()
    {
        if (!IsOrganiser())
        {
            return Unauthorized("Only organisers can view their events.");
        }

        List<RaceEvent> events = await _db.Events
            .Where(e => e.OrganiserId == CurrentUserId)
            .ToListAsync();

        return Ok(events);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent(RaceEvent raceEvent)
    {
        if (!IsOrganiser())
        {
            return Unauthorized("Only organisers can create events.");
        }

        if (raceEvent.EndDateTime <= raceEvent.StartDateTime)
        {
            return BadRequest("The end date must be after the start date.");
        }

        // The organiser ID comes from the session, not from the request body.
        raceEvent.EventId = 0;
        raceEvent.OrganiserId = CurrentUserId;
        raceEvent.Status = "Draft";
        raceEvent.CreatedAtUtc = DateTime.UtcNow;

        _db.Events.Add(raceEvent);
        await _db.SaveChangesAsync();

        return Ok("Event created successfully.");
    }

    [HttpPut("{eventId}")]
    public async Task<IActionResult> UpdateEvent(int eventId, RaceEvent newDetails)
    {
        if (!IsOrganiser())
        {
            return Unauthorized("Only organisers can update events.");
        }

        RaceEvent? raceEvent = await _db.Events.FindAsync(eventId);

        if (raceEvent == null || raceEvent.OrganiserId != CurrentUserId)
        {
            return NotFound("Your event was not found.");
        }

        if (raceEvent.Status == "Completed" || raceEvent.Status == "Cancelled")
        {
            return BadRequest("A completed or cancelled event cannot be changed.");
        }

        if (newDetails.EndDateTime <= newDetails.StartDateTime)
        {
            return BadRequest("The end date must be after the start date.");
        }

        raceEvent.Name = newDetails.Name;
        raceEvent.Description = newDetails.Description;
        raceEvent.EventType = newDetails.EventType;
        raceEvent.StartDateTime = newDetails.StartDateTime;
        raceEvent.EndDateTime = newDetails.EndDateTime;
        raceEvent.TimeZoneId = newDetails.TimeZoneId;
        raceEvent.VenueName = newDetails.VenueName;
        raceEvent.AddressLine1 = newDetails.AddressLine1;
        raceEvent.City = newDetails.City;
        raceEvent.Province = newDetails.Province;
        raceEvent.PostalCode = newDetails.PostalCode;
        raceEvent.RegistrationOpenUtc = newDetails.RegistrationOpenUtc;
        raceEvent.RegistrationCloseUtc = newDetails.RegistrationCloseUtc;
        raceEvent.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok("Event updated successfully.");
    }

    [HttpPut("{eventId}/status")]
    public async Task<IActionResult> ChangeStatus(int eventId, StatusChange newStatus)
    {
        if (!IsOrganiser())
        {
            return Unauthorized("Only organisers can change event status.");
        }

        RaceEvent? raceEvent = await _db.Events.FindAsync(eventId);

        if (raceEvent == null || raceEvent.OrganiserId != CurrentUserId)
        {
            return NotFound("Your event was not found.");
        }

        if (newStatus.Status != "Draft" && newStatus.Status != "Published" &&
            newStatus.Status != "Completed" && newStatus.Status != "Cancelled")
        {
            return BadRequest("Status must be Draft, Published, Completed or Cancelled.");
        }

        raceEvent.Status = newStatus.Status;
        raceEvent.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok("Event status changed successfully.");
    }

    [HttpDelete("{eventId}")]
    public async Task<IActionResult> DeleteEvent(int eventId)
    {
        if (!IsOrganiser())
        {
            return Unauthorized("Only organisers can delete events.");
        }

        RaceEvent? raceEvent = await _db.Events.FindAsync(eventId);

        if (raceEvent == null || raceEvent.OrganiserId != CurrentUserId)
        {
            return NotFound("Your event was not found.");
        }

        if (raceEvent.Status != "Draft")
        {
            return BadRequest("Only a draft event can be deleted.");
        }

        _db.Events.Remove(raceEvent);
        await _db.SaveChangesAsync();

        return Ok("Event deleted successfully.");
    }
}
