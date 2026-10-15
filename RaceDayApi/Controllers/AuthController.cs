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
    [Consumes("application/x-www-form-urlencoded")]
    public IActionResult Register([FromForm] RegistrationDetails details)
    {
        if (details.Role == null || !Enum.IsDefined(typeof(RegistrationRole), details.Role.Value))
            return BadRequest("Please choose Participant or Organiser.");
        if (string.IsNullOrWhiteSpace(details.FirstName) || string.IsNullOrWhiteSpace(details.LastName)
            || string.IsNullOrWhiteSpace(details.Email))
            return BadRequest("Please enter your first name, last name and email.");
        string role = details.Role.Value.ToString();
        string email = details.Email;
        string password = details.Password;

        // Check the two roles that are allowed in RaceDay.
        if (role != Roles.Organiser && role != Roles.Participant)
        {
            return BadRequest("Please choose Organiser or Participant as the role.");
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            return BadRequest("Password must have at least 8 characters.");
        }

        email = email.Trim().ToLower();

        if (_db.Users.Any(u => u.Email == email))
        {
            return Conflict("This email address is already registered.");
        }

        // Create a new User instance
        User user = new User
        {
            FirstName = details.FirstName.Trim(),
            LastName = details.LastName.Trim(),
            Email = email,
            Role = role,
            PasswordHash = _passwords.Hash(password),
            Password = string.Empty,
            UserId = 0,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Participants enter their own profile details in the registration form.
        if (role == Roles.Participant)
        {
            if (details.DateOfBirth == null || details.DateOfBirth.Value.Date >= DateTime.UtcNow.Date
                || string.IsNullOrWhiteSpace(details.Gender)
                || string.IsNullOrWhiteSpace(details.EmergencyContactName)
                || string.IsNullOrWhiteSpace(details.EmergencyContactPhone))
                return BadRequest("Participants must enter a past date of birth, gender, emergency contact name and phone.");

            user.ParticipantProfile = new ParticipantProfile
            {
                DateOfBirth = details.DateOfBirth.Value,
                Gender = details.Gender,
                EmergencyContactName = details.EmergencyContactName,
                EmergencyContactPhone = details.EmergencyContactPhone
            };
        }

        // Validate participant profile requirement
        if (role == Roles.Participant && user.ParticipantProfile == null)
        {
            return BadRequest("A participant must add their profile details.");
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return BadRequest("Password hashing failed.");
        }

        _db.Users.Add(user);
        _db.SaveChanges();

        return Ok("User registered successfully.");
    }

    [HttpPost("login")]
    [Consumes("application/x-www-form-urlencoded")]
    public IActionResult Login([FromForm] LoginDetails login)
    {
        if (string.IsNullOrWhiteSpace(login.Email) || string.IsNullOrWhiteSpace(login.Password))
            return BadRequest("Please enter your email and password.");
        string email = login.Email;
        string password = login.Password;
        email = email.Trim().ToLower();
        User? user = _db.Users.FirstOrDefault(u => u.Email == email);

        if (user == null || !user.IsActive || !_passwords.Verify(password, user.PasswordHash))
        {
            return Unauthorized("Incorrect email or password.");
        }

        // The session remembers who is logged in for the next API request.
        HttpContext.Session.Clear();
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
