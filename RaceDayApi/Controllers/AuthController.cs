using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
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
    public IActionResult Register(
        [FromForm, Required, MaxLength(80)] string firstName,
        [FromForm, Required, MaxLength(80)] string lastName,
        [FromForm, Required, EmailAddress, MaxLength(254)] string email,
        [FromForm, Required, MinLength(8), DataType(DataType.Password)] string password,
        [FromForm, Required, EnumDataType(typeof(RegistrationRole))] RegistrationRole? role = null,
        [FromForm, DataType(DataType.Date)] DateTime? dateOfBirth = null,
        [FromForm, MaxLength(20)] string? gender = null,
        [FromForm, MaxLength(160)] string? emergencyContactName = null,
        [FromForm, MaxLength(20)] string? emergencyContactPhone = null)
    {
        if (role == null || !Enum.IsDefined(typeof(RegistrationRole), role.Value))
        {
            return BadRequest("Please choose Participant or Organiser.");
        }
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName)
            || string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("Please enter your first name, last name and email.");
        }
        string roleName = role.Value.ToString();

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
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email,
            Role = roleName,
            PasswordHash = _passwords.Hash(password),
            Password = string.Empty,
            UserId = 0,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Participants enter their own profile details in the registration form.
        if (roleName == Roles.Participant)
        {
            if (dateOfBirth == null || dateOfBirth.Value.Date >= DateTime.UtcNow.Date
                || string.IsNullOrWhiteSpace(gender)
                || string.IsNullOrWhiteSpace(emergencyContactName)
                || string.IsNullOrWhiteSpace(emergencyContactPhone))
            {
                return BadRequest("Participants must enter a past date of birth, gender, emergency contact name and phone.");
            }

            user.ParticipantProfile = new ParticipantProfile
            {
                DateOfBirth = dateOfBirth.Value,
                Gender = gender,
                EmergencyContactName = emergencyContactName,
                EmergencyContactPhone = emergencyContactPhone
            };
        }

        _db.Users.Add(user);
        _db.SaveChanges();

        return Ok("User registered successfully.");
    }

    [HttpPost("login")]
    public IActionResult Login(
        [FromForm, Required, EmailAddress] string email,
        [FromForm, Required, DataType(DataType.Password)] string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return BadRequest("Please enter your email and password.");
        }
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
