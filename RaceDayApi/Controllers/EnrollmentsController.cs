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
    public IActionResult Enrol(int eventId, EventEnrollment enrollment)
    {
        if (!IsParticipant())
        {
            return Unauthorized("Only participants can enter an event.");
        }

        if (!enrollment.EmergencyConsent)
        {
            return BadRequest("Emergency consent is required.");
        }

        RaceEvent? raceEvent = _db.Events.Find(eventId);
        Category? category = _db.Categories.Find(enrollment.CategoryId);

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

        bool alreadyEntered = _db.EventEnrollments.Any(e =>
            e.EventId == eventId && e.ParticipantId == CurrentUserId);

        if (alreadyEntered)
        {
            return BadRequest("You are already entered for this event.");
        }

        int numberOfEntries = _db.EventEnrollments.Count(e =>
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
        _db.SaveChanges();

        return Ok("Event entry recorded successfully.");
    }

    [HttpGet("enrollments/me")]
    public IActionResult GetMyEnrolments()
    {
        if (!IsParticipant())
        {
            return Unauthorized("Only participants can view their enrolments.");
        }

        List<EventEnrollment> enrolments = _db.EventEnrollments
            .Include(e => e.Event)
            .Include(e => e.Category)
            .Where(e => e.ParticipantId == CurrentUserId)
            .ToList();

        return Ok(enrolments);
    }

    [HttpGet("events/{eventId}/enrollments")]
    public IActionResult GetEventEnrolments(int eventId)
    {
        if (!IsOrganiser())
        {
            return Unauthorized("Only organisers can view event enrolments.");
        }

        RaceEvent? raceEvent = _db.Events.Find(eventId);

        if (raceEvent == null || raceEvent.OrganiserId != CurrentUserId)
        {
            return NotFound("Your event was not found.");
        }

        List<EventEnrollment> enrolments = _db.EventEnrollments
            .Include(e => e.Participant)
            .Include(e => e.Category)
            .Where(e => e.EventId == eventId)
            .ToList();

        return Ok(enrolments);
    }
}
