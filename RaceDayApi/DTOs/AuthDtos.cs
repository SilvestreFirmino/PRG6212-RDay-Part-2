using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.DTOs;

public class RegisterRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8)] public string Password { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string FirstName { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string LastName { get; set; } = string.Empty;
    [MaxLength(20)] public string? PhoneNumber { get; set; }
    [Required] public string Role { get; set; } = string.Empty;
    public ParticipantProfileRequest? ParticipantProfile { get; set; }
}

public class ParticipantProfileRequest
{
    public DateTime DateOfBirth { get; set; }
    [Required, MaxLength(20)] public string Gender { get; set; } = string.Empty;
    [Required, MaxLength(160)] public string EmergencyContactName { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string EmergencyContactPhone { get; set; } = string.Empty;
    [MaxLength(500)] public string? MedicalNotes { get; set; }
    [MaxLength(120)] public string? ClubName { get; set; }
}

public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required] public string CurrentPassword { get; set; } = string.Empty;
    [Required, MinLength(8)] public string NewPassword { get; set; } = string.Empty;
}

public class ProfileUpdateRequest
{
    [Required, MaxLength(80)] public string FirstName { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string LastName { get; set; } = string.Empty;
    [MaxLength(20)] public string? PhoneNumber { get; set; }
    public ParticipantProfileRequest? ParticipantProfile { get; set; }
}

public class EmailUpdateRequest
{
    [Required, EmailAddress] public string NewEmail { get; set; } = string.Empty;
    [Required] public string CurrentPassword { get; set; } = string.Empty;
}

public record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, UserSummary User);
public record UserSummary(int UserId, string Email, string FirstName, string LastName, string? PhoneNumber, string Role, bool IsActive);
