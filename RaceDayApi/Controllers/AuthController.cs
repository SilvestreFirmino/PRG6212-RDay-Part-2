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
    private readonly JwtTokenService _tokens;
    public AuthController(RaceDayDbContext db, PasswordService passwords, JwtTokenService tokens) => (_db, _passwords, _tokens) = (db, passwords, tokens);

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
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
        return CreatedAtAction(nameof(Login), _tokens.Create(user));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        string email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email);
        if (user is null || !user.IsActive || !_passwords.Verify(request.Password, user.PasswordHash)) return Unauthorized("Invalid credentials.");
        return Ok(_tokens.Create(user));
    }
}
