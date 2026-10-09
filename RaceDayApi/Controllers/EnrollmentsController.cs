using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.DTOs;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[Route("api")]
public class EnrollmentsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    public EnrollmentsController(RaceDayDbContext db) => _db = db;

    [HttpPost("events/{eventId:int}/enrollments")]
    public async Task<ActionResult<EventEnrollment>> Enrol(int eventId, EnrollmentRequest request)
    {
        if (!IsParticipant()) return Unauthorized("Only participants can enter an event.");
        if (!request.EmergencyConsent) return BadRequest("Emergency consent is required.");
        var category = await _db.Categories.Include(c => c.Event).SingleOrDefaultAsync(c => c.CategoryId == request.CategoryId && c.EventId == eventId && c.IsActive);
        if (category is null || category.Event is null) return NotFound("The selected category was not found.");
        if (category.Event.Status != "Published" || DateTime.UtcNow < category.Event.RegistrationOpenUtc || DateTime.UtcNow > category.Event.RegistrationCloseUtc) return Conflict("Registration is not open.");
        if (await _db.EventEnrollments.AnyAsync(e => e.EventId == eventId && e.ParticipantId == CurrentUserId)) return Conflict("You are already enrolled in this event.");
        if (await _db.EventEnrollments.CountAsync(e => e.CategoryId == category.CategoryId && e.Status != "Withdrawn") >= category.Capacity) return Conflict("The category is full.");
        var enrollment = new EventEnrollment { EventId = eventId, CategoryId = category.CategoryId, ParticipantId = CurrentUserId, FeePaid = category.EntryFee, EmergencyConsent = true };
        _db.EventEnrollments.Add(enrollment); await _db.SaveChangesAsync(); return CreatedAtAction(nameof(Get), new { enrollmentId = enrollment.EnrollmentId }, enrollment);
    }

    [HttpGet("enrollments/me")]
    public async Task<ActionResult<IEnumerable<EventEnrollment>>> Mine()
    {
        if (!IsParticipant()) return Unauthorized("Only participants can view their enrolments.");
        return Ok(await _db.EventEnrollments.Include(e => e.Event).Include(e => e.Category).Where(e => e.ParticipantId == CurrentUserId).OrderByDescending(e => e.EnrolledAtUtc).ToListAsync());
    }

    [HttpGet("enrollments/{enrollmentId:int}")]
    public async Task<ActionResult<EventEnrollment>> Get(int enrollmentId)
    {
        if (!LoggedIn()) return Unauthorized("Please log in first.");
        var enrollment = await _db.EventEnrollments.Include(e => e.Event).Include(e => e.Category).SingleOrDefaultAsync(e => e.EnrollmentId == enrollmentId);
        if (enrollment is null) return NotFound();
        bool canView = enrollment.ParticipantId == CurrentUserId || (IsOrganiser() && enrollment.Event?.OrganiserId == CurrentUserId);
        return canView ? Ok(enrollment) : Unauthorized("You cannot view this enrolment.");
    }

    [HttpPatch("enrollments/{enrollmentId:int}/withdraw")]
    public async Task<IActionResult> Withdraw(int enrollmentId)
    {
        if (!IsParticipant()) return Unauthorized("Only participants can withdraw an enrolment.");
        var enrollment = await _db.EventEnrollments.Include(e => e.Event).SingleOrDefaultAsync(e => e.EnrollmentId == enrollmentId && e.ParticipantId == CurrentUserId); if (enrollment is null) return NotFound();
        if (enrollment.Status == "Withdrawn" || enrollment.Event!.RegistrationCloseUtc < DateTime.UtcNow) return Conflict("This enrolment cannot be withdrawn.");
        enrollment.Status = "Withdrawn"; enrollment.UpdatedAtUtc = DateTime.UtcNow; await _db.SaveChangesAsync(); return NoContent();
    }

    [HttpGet("events/{eventId:int}/enrollments")]
    public async Task<ActionResult<IEnumerable<EventEnrollment>>> EventEnrollments(int eventId)
    {
        if (!IsOrganiser()) return Unauthorized("Only organisers can view event enrolments.");
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId && e.OrganiserId == CurrentUserId)) return NotFound();
        return Ok(await _db.EventEnrollments.Include(e => e.Category).Include(e => e.Participant).Where(e => e.EventId == eventId).ToListAsync());
    }

    [HttpPatch("enrollments/{enrollmentId:int}")]
    public async Task<IActionResult> Update(int enrollmentId, EnrollmentUpdateRequest request)
    {
        if (!IsOrganiser()) return Unauthorized("Only organisers can update enrolments.");
        var enrollment = await _db.EventEnrollments.Include(e => e.Event).SingleOrDefaultAsync(e => e.EnrollmentId == enrollmentId && e.Event!.OrganiserId == CurrentUserId); if (enrollment is null) return NotFound();
        if (request.BibNumber is not null) enrollment.BibNumber = request.BibNumber.Trim(); if (request.Status is not null) enrollment.Status = request.Status.Trim(); if (request.PaymentStatus is not null) enrollment.PaymentStatus = request.PaymentStatus.Trim(); enrollment.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(); return NoContent();
    }
}
