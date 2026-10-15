using System.ComponentModel.DataAnnotations;
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
    public IActionResult UpdateProfile(
        [FromForm, Required, MaxLength(80)] string firstName,
        [FromForm, Required, MaxLength(80)] string lastName,
        [FromForm, MaxLength(20)] string? phoneNumber = null,
        [FromForm, DataType(DataType.Date)] DateTime? dateOfBirth = null,
        [FromForm, MaxLength(20)] string? gender = null,
        [FromForm, MaxLength(160)] string? emergencyContactName = null,
        [FromForm, MaxLength(20)] string? emergencyContactPhone = null,
        [FromForm, MaxLength(500)] string? medicalNotes = null,
        [FromForm, MaxLength(120)] string? clubName = null)
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
        user.FirstName = firstName;
        user.LastName = lastName;
        user.PhoneNumber = phoneNumber;
        user.UpdatedAtUtc = DateTime.UtcNow;

        if (user.Role == Roles.Participant && user.ParticipantProfile != null)
        {
            if (dateOfBirth.HasValue)
            {
                if (dateOfBirth.Value.Date >= DateTime.UtcNow.Date)
                    return BadRequest("Date of birth must be in the past.");
                user.ParticipantProfile.DateOfBirth = dateOfBirth.Value;
            }
            if (!string.IsNullOrWhiteSpace(gender))
                user.ParticipantProfile.Gender = gender;
            if (!string.IsNullOrWhiteSpace(emergencyContactName))
                user.ParticipantProfile.EmergencyContactName = emergencyContactName;
            if (!string.IsNullOrWhiteSpace(emergencyContactPhone))
                user.ParticipantProfile.EmergencyContactPhone = emergencyContactPhone;
            user.ParticipantProfile.MedicalNotes = medicalNotes;
            user.ParticipantProfile.ClubName = clubName;
        }

        _db.SaveChanges();
        return Ok("Profile updated successfully.");
    }
}
