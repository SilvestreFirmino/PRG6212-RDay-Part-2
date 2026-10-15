using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[Route("api/profile")]
public class ProfileController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public ProfileController(RaceDayDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult GetProfile()
    {
        if (!LoggedIn())
        {
            return Unauthorized("Please log in first.");
        }

        User? user = _db.Users.Include(u => u.ParticipantProfile)
            .FirstOrDefault(u => u.UserId == CurrentUserId);

        if (user == null)
        {
            return NotFound("User not found.");
        }

        return Ok(user);
    }

    [HttpPut]
    [Consumes("application/x-www-form-urlencoded")]
    public IActionResult UpdateProfile([FromForm] ProfileForm newDetails)
    {
        if (!LoggedIn())
        {
            return Unauthorized("Please log in first.");
        }

        User? user = _db.Users.Include(u => u.ParticipantProfile)
            .FirstOrDefault(u => u.UserId == CurrentUserId);

        if (user == null)
        {
            return NotFound("User not found.");
        }

        // Only update the details belonging to the logged-in user.
        user.FirstName = newDetails.FirstName;
        user.LastName = newDetails.LastName;
        user.PhoneNumber = newDetails.PhoneNumber;
        user.UpdatedAtUtc = DateTime.UtcNow;

        if (user.Role == Roles.Participant && user.ParticipantProfile != null)
        {
            if (newDetails.DateOfBirth.HasValue)
            {
                if (newDetails.DateOfBirth.Value.Date >= DateTime.UtcNow.Date)
                    return BadRequest("Date of birth must be in the past.");
                user.ParticipantProfile.DateOfBirth = newDetails.DateOfBirth.Value;
            }
            if (!string.IsNullOrWhiteSpace(newDetails.Gender))
                user.ParticipantProfile.Gender = newDetails.Gender;
            if (!string.IsNullOrWhiteSpace(newDetails.EmergencyContactName))
                user.ParticipantProfile.EmergencyContactName = newDetails.EmergencyContactName;
            if (!string.IsNullOrWhiteSpace(newDetails.EmergencyContactPhone))
                user.ParticipantProfile.EmergencyContactPhone = newDetails.EmergencyContactPhone;
            user.ParticipantProfile.MedicalNotes = newDetails.MedicalNotes;
            user.ParticipantProfile.ClubName = newDetails.ClubName;
        }

        _db.SaveChanges();
        return Ok("Profile updated successfully.");
    }
}
