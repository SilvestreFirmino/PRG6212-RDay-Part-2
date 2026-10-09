using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.DTOs;
using RaceDayApi.Models;
using RaceDayApi.Services;

namespace RaceDayApi.Controllers;

[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly PasswordService _passwords;
    public AuthController(RaceDayDbContext db, PasswordService passwords) => (_db, _passwords) = (db, passwords);

    [HttpPost("register")]
    public async Task<ActionResult> Register(RegisterRequest request)
    {
        string role = request.Role.Trim();
        if (role != Roles.Organiser && role != Roles.Participant) return BadRequest("Role must be Organiser or Participant.");
        if (role == Roles.Participant && request.ParticipantProfile is null) return BadRequest("Participants require profile details.");
        string email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email)) return Conflict("An account with this email already exists.");

        var user = new User { Email = email, PasswordHash = _passwords.Hash(request.Password), FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), PhoneNumber = request.PhoneNumber?.Trim(), Role = role };
        if (request.ParticipantProfile is not null)
        {
            var p = request.ParticipantProfile;
            user.ParticipantProfile = new ParticipantProfile { DateOfBirth = p.DateOfBirth.Date, Gender = p.Gender.Trim(), EmergencyContactName = p.EmergencyContactName.Trim(), EmergencyContactPhone = p.EmergencyContactPhone.Trim(), MedicalNotes = p.MedicalNotes?.Trim(), ClubName = p.ClubName?.Trim() };
        }
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Ok("User registered successfully.");
    }

    [HttpPost("login")]
    public async Task<ActionResult> Login(LoginRequest request)
    {
        string email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email);
        if (user is null || !user.IsActive || !_passwords.Verify(request.Password, user.PasswordHash)) return Unauthorized("Incorrect email or password.");

        // Save the user's details in the session. Other controllers read these values.
        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("Role", user.Role);
        HttpContext.Session.SetString("UserName", user.FirstName);
        return Ok($"Welcome {user.FirstName}. You are logged in as {user.Role}.");
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok("You are logged out.");
    }
}
