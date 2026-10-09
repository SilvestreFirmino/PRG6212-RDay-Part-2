using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.DTOs;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[Route("api")]
public class ResultsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    public ResultsController(RaceDayDbContext db) => _db = db;

    [HttpGet("events/{eventId:int}/results")]
    public async Task<ActionResult<IEnumerable<RaceResult>>> List(int eventId)
    {
        var eventItem = await _db.Events.FindAsync(eventId);
        if (eventItem is null) return NotFound();
        if (eventItem.Status != "Completed") return Unauthorized("Results are only public after the event is completed.");
        return Ok(await _db.Results.Include(r => r.Enrollment).Where(r => r.Enrollment!.EventId == eventId).OrderBy(r => r.DurationMilliseconds).ToListAsync());
    }

    [HttpGet("results/{resultId:int}")]
    public async Task<ActionResult<RaceResult>> Get(int resultId)
    {
        var result = await _db.Results.Include(r => r.Enrollment).ThenInclude(e => e!.Event).SingleOrDefaultAsync(r => r.ResultId == resultId);
        return result is null || result.Enrollment?.Event?.Status != "Completed" ? NotFound() : Ok(result);
    }

    [HttpGet("results/me")]
    public async Task<ActionResult<IEnumerable<RaceResult>>> Mine()
    {
        if (!IsParticipant()) return Unauthorized("Only participants can view their results.");
        return Ok(await _db.Results.Include(r => r.Enrollment).Where(r => r.Enrollment!.ParticipantId == CurrentUserId).OrderByDescending(r => r.RecordedAtUtc).ToListAsync());
    }

    [HttpPost("enrollments/{enrollmentId:int}/result")]
    public async Task<ActionResult<RaceResult>> Create(int enrollmentId, ResultRequest request)
    {
        if (!IsOrganiser()) return Unauthorized("Only organisers can capture results.");
        var enrollment = await OwnedEnrollment(enrollmentId); if (enrollment is null) return NotFound();
        if (enrollment.Event!.Status != "Completed") return Conflict("Results can only be captured for completed events.");
        if (await _db.Results.AnyAsync(r => r.EnrollmentId == enrollmentId)) return Conflict("A result already exists for this enrolment.");
        if (!ValidResult(request)) return BadRequest("Finished results require duration and positions.");
        var result = new RaceResult { EnrollmentId = enrollmentId, RecordedByOrganiserId = CurrentUserId, ResultStatus = request.ResultStatus.Trim(), DurationMilliseconds = request.DurationMilliseconds, OverallPosition = request.OverallPosition, CategoryPosition = request.CategoryPosition, Notes = request.Notes?.Trim() };
        _db.Results.Add(result); await _db.SaveChangesAsync(); return CreatedAtAction(nameof(Get), new { resultId = result.ResultId }, result);
    }

    [HttpPut("results/{resultId:int}")]
    public async Task<IActionResult> Update(int resultId, ResultRequest request)
    {
        if (!IsOrganiser()) return Unauthorized("Only organisers can update results.");
        var result = await _db.Results.Include(r => r.Enrollment).ThenInclude(e => e!.Event).SingleOrDefaultAsync(r => r.ResultId == resultId && r.Enrollment!.Event!.OrganiserId == CurrentUserId); if (result is null) return NotFound();
        if (!ValidResult(request)) return BadRequest("Finished results require duration and positions.");
        result.ResultStatus = request.ResultStatus.Trim(); result.DurationMilliseconds = request.DurationMilliseconds; result.OverallPosition = request.OverallPosition; result.CategoryPosition = request.CategoryPosition; result.Notes = request.Notes?.Trim(); result.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(); return NoContent();
    }

    [HttpDelete("results/{resultId:int}")]
    public async Task<IActionResult> Delete(int resultId)
    {
        if (!IsOrganiser()) return Unauthorized("Only organisers can delete results.");
        var result = await _db.Results.Include(r => r.Enrollment).ThenInclude(e => e!.Event).SingleOrDefaultAsync(r => r.ResultId == resultId && r.Enrollment!.Event!.OrganiserId == CurrentUserId); if (result is null) return NotFound();
        _db.Results.Remove(result); await _db.SaveChangesAsync(); return NoContent();
    }

    private Task<EventEnrollment?> OwnedEnrollment(int enrollmentId) => _db.EventEnrollments.Include(e => e.Event).SingleOrDefaultAsync(e => e.EnrollmentId == enrollmentId && e.Event!.OrganiserId == CurrentUserId);
    private static bool ValidResult(ResultRequest request) => request.ResultStatus != "Finished" || (request.DurationMilliseconds.HasValue && request.OverallPosition.HasValue && request.CategoryPosition.HasValue);
}
