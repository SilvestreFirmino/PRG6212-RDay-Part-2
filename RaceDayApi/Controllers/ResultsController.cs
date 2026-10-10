using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[Route("api")]
public class ResultsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db)
    {
        _db = db;
    }

    [HttpGet("events/{eventId}/results")]
    public async Task<IActionResult> GetResults(int eventId)
    {
        List<RaceResult> results = await _db.Results
            .Include(r => r.Enrollment)
            .Where(r => r.Enrollment != null && r.Enrollment.EventId == eventId)
            .OrderBy(r => r.OverallPosition)
            .ToListAsync();

        return Ok(results);
    }

    [HttpGet("results/me")]
    public async Task<IActionResult> GetMyResults()
    {
        if (!IsParticipant())
        {
            return Unauthorized("Only participants can view their results.");
        }

        List<RaceResult> results = await _db.Results
            .Include(r => r.Enrollment)
            .Where(r => r.Enrollment != null && r.Enrollment.ParticipantId == CurrentUserId)
            .ToListAsync();

        return Ok(results);
    }

    [HttpPost("enrollments/{enrollmentId}/result")]
    public async Task<IActionResult> CreateResult(int enrollmentId, RaceResult result)
    {
        if (!IsOrganiser())
        {
            return Unauthorized("Only organisers can capture results.");
        }

        EventEnrollment? enrollment = await _db.EventEnrollments
            .Include(e => e.Event)
            .FirstOrDefaultAsync(e => e.EnrollmentId == enrollmentId);

        if (enrollment == null || enrollment.Event == null || enrollment.Event.OrganiserId != CurrentUserId)
        {
            return NotFound("The enrolment was not found.");
        }

        if (enrollment.Event.Status != "Completed")
        {
            return BadRequest("Complete the event before capturing results.");
        }

        if (await _db.Results.AnyAsync(r => r.EnrollmentId == enrollmentId))
        {
            return BadRequest("This enrolment already has a result.");
        }

        if (result.ResultStatus == "Finished" &&
            (!result.DurationMilliseconds.HasValue || !result.OverallPosition.HasValue))
        {
            return BadRequest("A finished result needs a time and overall position.");
        }

        result.ResultId = 0;
        result.EnrollmentId = enrollmentId;
        result.RecordedByOrganiserId = CurrentUserId;
        result.RecordedAtUtc = DateTime.UtcNow;

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return Ok("Result captured successfully.");
    }

    [HttpPut("results/{resultId}")]
    public async Task<IActionResult> UpdateResult(int resultId, RaceResult newDetails)
    {
        if (!IsOrganiser())
        {
            return Unauthorized("Only organisers can update results.");
        }

        RaceResult? result = await _db.Results
            .Include(r => r.Enrollment)
            .ThenInclude(e => e!.Event)
            .FirstOrDefaultAsync(r => r.ResultId == resultId);

        if (result == null || result.Enrollment == null || result.Enrollment.Event == null ||
            result.Enrollment.Event.OrganiserId != CurrentUserId)
        {
            return NotFound("Your result was not found.");
        }

        result.ResultStatus = newDetails.ResultStatus;
        result.DurationMilliseconds = newDetails.DurationMilliseconds;
        result.OverallPosition = newDetails.OverallPosition;
        result.CategoryPosition = newDetails.CategoryPosition;
        result.Notes = newDetails.Notes;
        result.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok("Result updated successfully.");
    }
}
