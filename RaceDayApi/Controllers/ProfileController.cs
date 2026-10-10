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
    public async Task<IActionResult> GetProfile()
    {
        if (!LoggedIn())
        {
            return Unauthorized("Please log in first.");
        }

        User? user = await _db.Users.Include(u => u.ParticipantProfile)
            .FirstOrDefaultAsync(u => u.UserId == CurrentUserId);

        if (user == null)
        {
            return NotFound("User not found.");
        }

        return Ok(user);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile(User newDetails)
    {
        if (!LoggedIn())
        {
            return Unauthorized("Please log in first.");
        }

        User? user = await _db.Users.Include(u => u.ParticipantProfile)
            .FirstOrDefaultAsync(u => u.UserId == CurrentUserId);

        if (user == null)
        {
            return NotFound("User not found.");
        }

        // Only update the details belonging to the logged-in user.
        user.FirstName = newDetails.FirstName;
        user.LastName = newDetails.LastName;
        user.PhoneNumber = newDetails.PhoneNumber;
        user.UpdatedAtUtc = DateTime.UtcNow;

        if (user.Role == Roles.Participant && user.ParticipantProfile != null && newDetails.ParticipantProfile != null)
        {
            user.ParticipantProfile.DateOfBirth = newDetails.ParticipantProfile.DateOfBirth;
            user.ParticipantProfile.Gender = newDetails.ParticipantProfile.Gender;
            user.ParticipantProfile.EmergencyContactName = newDetails.ParticipantProfile.EmergencyContactName;
            user.ParticipantProfile.EmergencyContactPhone = newDetails.ParticipantProfile.EmergencyContactPhone;
            user.ParticipantProfile.MedicalNotes = newDetails.ParticipantProfile.MedicalNotes;
            user.ParticipantProfile.ClubName = newDetails.ParticipantProfile.ClubName;
        }

        await _db.SaveChangesAsync();
        return Ok("Profile updated successfully.");
    }
}
