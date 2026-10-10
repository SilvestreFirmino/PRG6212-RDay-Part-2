using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Models;
using RaceDayApi.Services;

namespace RaceDayApi.Controllers;

[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly PasswordService _passwords;

    public AuthController(RaceDayDbContext db, PasswordService passwords)
    {
        _db = db;
        _passwords = passwords;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(User user)
    {
        // Check the two roles that are allowed in RaceDay.
        if (user.Role != Roles.Organiser && user.Role != Roles.Participant)
        {
            return BadRequest("Please choose Organiser or Participant as the role.");
        }

        if (string.IsNullOrWhiteSpace(user.Password) || user.Password.Length < 8)
        {
            return BadRequest("Password must have at least 8 characters.");
        }

        if (user.Role == Roles.Participant && user.ParticipantProfile == null)
        {
            return BadRequest("A participant must add their profile details.");
        }

        user.Email = user.Email.Trim().ToLower();

        if (await _db.Users.AnyAsync(u => u.Email == user.Email))
        {
            return Conflict("This email address is already registered.");
        }

        // Do not save the plain password. Save the protected version instead.
        user.PasswordHash = _passwords.Hash(user.Password);
        user.Password = string.Empty;
        user.UserId = 0;
        user.IsActive = true;
        user.CreatedAtUtc = DateTime.UtcNow;

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok("User registered successfully.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDetails login)
    {
        string email = login.Email.Trim().ToLower();
        User? user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !user.IsActive || !_passwords.Verify(login.Password, user.PasswordHash))
        {
            return Unauthorized("Incorrect email or password.");
        }

        // The session remembers who is logged in for the next API request.
        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("Role", user.Role);

        return Ok("Login successful. You are logged in as " + user.Role + ".");
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok("You are logged out.");
    }
}
