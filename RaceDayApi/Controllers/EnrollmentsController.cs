using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[Route("api")]
public class EnrollmentsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrollmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    [HttpPost("events/{eventId}/enrollments")]
    public async Task<IActionResult> Enrol(int eventId, EventEnrollment enrollment)
    {
        if (!IsParticipant())
        {
            return Unauthorized("Only participants can enter an event.");
        }

        if (!enrollment.EmergencyConsent)
        {
            return BadRequest("Emergency consent is required.");
        }

        RaceEvent? raceEvent = await _db.Events.FindAsync(eventId);
        Category? category = await _db.Categories.FindAsync(enrollment.CategoryId);

        if (raceEvent == null || category == null || category.EventId != eventId)
        {
            return NotFound("The event or category was not found.");
        }

        if (raceEvent.Status != "Published")
        {
            return BadRequest("This event is not open for entries.");
        }

        if (DateTime.UtcNow < raceEvent.RegistrationOpenUtc || DateTime.UtcNow > raceEvent.RegistrationCloseUtc)
        {
            return BadRequest("Registration is currently closed.");
        }

        bool alreadyEntered = await _db.EventEnrollments.AnyAsync(e =>
            e.EventId == eventId && e.ParticipantId == CurrentUserId);

        if (alreadyEntered)
        {
            return BadRequest("You are already entered for this event.");
        }

        int numberOfEntries = await _db.EventEnrollments.CountAsync(e =>
            e.CategoryId == category.CategoryId && e.Status != "Withdrawn");

        if (numberOfEntries >= category.Capacity)
        {
            return BadRequest("This category is full.");
        }

        // IDs and payment amount are set by the API, not trusted from the request body.
        enrollment.EnrollmentId = 0;
        enrollment.EventId = eventId;
        enrollment.ParticipantId = CurrentUserId;
        enrollment.FeePaid = category.EntryFee;
        enrollment.Status = "Pending";
        enrollment.PaymentStatus = "Unpaid";
        enrollment.EnrolledAtUtc = DateTime.UtcNow;

        _db.EventEnrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        return Ok("Event entry recorded successfully.");
    }

    [HttpGet("enrollments/me")]
    public async Task<IActionResult> GetMyEnrolments()
    {
        if (!IsParticipant())
        {
            return Unauthorized("Only participants can view their enrolments.");
        }

        List<EventEnrollment> enrolments = await _db.EventEnrollments
            .Include(e => e.Event)
            .Include(e => e.Category)
            .Where(e => e.ParticipantId == CurrentUserId)
            .ToListAsync();

        return Ok(enrolments);
    }

    [HttpGet("events/{eventId}/enrollments")]
    public async Task<IActionResult> GetEventEnrolments(int eventId)
    {
        if (!IsOrganiser())
        {
            return Unauthorized("Only organisers can view event enrolments.");
        }

        RaceEvent? raceEvent = await _db.Events.FindAsync(eventId);

        if (raceEvent == null || raceEvent.OrganiserId != CurrentUserId)
        {
            return NotFound("Your event was not found.");
        }

        List<EventEnrollment> enrolments = await _db.EventEnrollments
            .Include(e => e.Participant)
            .Include(e => e.Category)
            .Where(e => e.EventId == eventId)
            .ToListAsync();

        return Ok(enrolments);
    }
}
