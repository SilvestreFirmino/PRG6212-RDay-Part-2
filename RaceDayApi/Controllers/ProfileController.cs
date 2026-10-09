using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.DTOs;
using RaceDayApi.Models;
using RaceDayApi.Services;

namespace RaceDayApi.Controllers;

[Route("api/profile")]
public class ProfileController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly PasswordService _passwords;
    public ProfileController(RaceDayDbContext db, PasswordService passwords) => (_db, _passwords) = (db, passwords);

    [HttpGet]
    public async Task<ActionResult<object>> Get()
    {
        if (!LoggedIn()) return Unauthorized("Please log in first.");
        var user = await _db.Users.Include(u => u.ParticipantProfile).SingleOrDefaultAsync(u => u.UserId == CurrentUserId);
        return user is null ? NotFound() : Ok(new { user.UserId, user.Email, user.FirstName, user.LastName, user.PhoneNumber, user.Role, user.IsActive, participantProfile = user.ParticipantProfile });
    }

    [HttpPut]
    public async Task<IActionResult> Update(ProfileUpdateRequest request)
    {
        if (!LoggedIn()) return Unauthorized("Please log in first.");
        var user = await _db.Users.Include(u => u.ParticipantProfile).SingleOrDefaultAsync(u => u.UserId == CurrentUserId);
        if (user is null) return NotFound();
        user.FirstName = request.FirstName.Trim(); user.LastName = request.LastName.Trim(); user.PhoneNumber = request.PhoneNumber?.Trim(); user.UpdatedAtUtc = DateTime.UtcNow;
        if (user.Role == Roles.Participant && request.ParticipantProfile is not null && user.ParticipantProfile is not null)
        {
            var p = request.ParticipantProfile; user.ParticipantProfile.DateOfBirth = p.DateOfBirth.Date; user.ParticipantProfile.Gender = p.Gender.Trim(); user.ParticipantProfile.EmergencyContactName = p.EmergencyContactName.Trim(); user.ParticipantProfile.EmergencyContactPhone = p.EmergencyContactPhone.Trim(); user.ParticipantProfile.MedicalNotes = p.MedicalNotes?.Trim(); user.ParticipantProfile.ClubName = p.ClubName?.Trim();
        }
        await _db.SaveChangesAsync(); return NoContent();
    }

    [HttpPut("email")]
    public async Task<IActionResult> ChangeEmail(EmailUpdateRequest request)
    {
        if (!LoggedIn()) return Unauthorized("Please log in first.");
        var user = await _db.Users.SingleOrDefaultAsync(u => u.UserId == CurrentUserId);
        if (user is null || !_passwords.Verify(request.CurrentPassword, user.PasswordHash)) return Unauthorized("Current password is invalid.");
        string email = request.NewEmail.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email && u.UserId != CurrentUserId)) return Conflict("An account with this email already exists.");
        user.Email = email; user.UpdatedAtUtc = DateTime.UtcNow; await _db.SaveChangesAsync(); return NoContent();
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        if (!LoggedIn()) return Unauthorized("Please log in first.");
        var user = await _db.Users.SingleOrDefaultAsync(u => u.UserId == CurrentUserId);
        if (user is null || !_passwords.Verify(request.CurrentPassword, user.PasswordHash)) return Unauthorized("Current password is invalid.");
        user.PasswordHash = _passwords.Hash(request.NewPassword); user.UpdatedAtUtc = DateTime.UtcNow; await _db.SaveChangesAsync(); return NoContent();
    }
}
