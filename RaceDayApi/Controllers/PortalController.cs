using Microsoft.AspNetCore.Mvc;
using RaceDayApi.Data;

namespace RaceDayApi.Controllers;

// Small read-only responses for the website. All joins and ownership checks stay in the API.
[Route("api/portal")]
public class PortalController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    public PortalController(RaceDayDbContext db) { _db = db; }

    [HttpGet("organiser")]
    public IActionResult Dashboard()
    {
        if (!IsOrganiser()) return Unauthorized("Please sign in as an Organiser.");
        return Ok(_db.Events.Where(e => e.OrganiserId == CurrentUserId).OrderBy(e => e.StartDateTime)
            .Select(e => new { e.EventId, e.Name, e.City, e.StartDateTime, e.Status, e.BannerUrl,
                EnrolmentCount = e.Enrollments.Count }).ToList());
    }

    [HttpGet("enrolments")]
    public IActionResult Mine()
    {
        if (!IsParticipant()) return Unauthorized("Please sign in as a Participant.");
        return Ok(_db.EventEnrollments.Where(e => e.ParticipantId == CurrentUserId)
            .Select(e => new { e.EnrollmentId, e.EventId, EventName = e.Event!.Name,
                e.Event.StartDateTime, CategoryName = e.Category!.Name, e.Status, e.FeePaid }).ToList());
    }

    [HttpGet("results")]
    public IActionResult MyResults()
    {
        if (!IsParticipant()) return Unauthorized("Please sign in as a Participant.");
        return Ok(_db.Results.Where(r => r.Enrollment!.ParticipantId == CurrentUserId)
            .Select(r => new { r.ResultId, r.EnrollmentId, r.Enrollment!.EventId,
                EventName = r.Enrollment.Event!.Name, CategoryName = r.Enrollment.Category!.Name,
                r.ResultStatus, r.DurationMilliseconds, r.OverallPosition, r.CategoryPosition, r.Notes }).ToList());
    }

    [HttpGet("events/{eventId}/enrolments")]
    public IActionResult EventEntries(int eventId)
    {
        if (!IsOrganiser()) return Unauthorized("Please sign in as an Organiser.");
        if (!_db.Events.Any(e => e.EventId == eventId && e.OrganiserId == CurrentUserId)) return NotFound();
        return Ok(_db.EventEnrollments.Where(e => e.EventId == eventId).Select(e => new {
            e.EnrollmentId, e.EventId, EventName = e.Event!.Name, e.Event.StartDateTime,
            ParticipantName = e.Participant!.FirstName + " " + e.Participant.LastName,
            CategoryName = e.Category!.Name, e.Status, e.FeePaid,
            ResultId = e.Result == null ? (int?)null : e.Result.ResultId,
            ResultStatus = e.Result == null ? null : e.Result.ResultStatus,
            DurationMilliseconds = e.Result == null ? null : e.Result.DurationMilliseconds,
            OverallPosition = e.Result == null ? null : e.Result.OverallPosition,
            CategoryPosition = e.Result == null ? null : e.Result.CategoryPosition,
            Notes = e.Result == null ? null : e.Result.Notes
        }).ToList());
    }
}
