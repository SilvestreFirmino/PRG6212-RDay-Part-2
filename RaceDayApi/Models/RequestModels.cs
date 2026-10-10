using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.Models;

// These two small classes are used only where a full database model would not make sense.
public class LoginDetails
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class StatusChange
{
    [Required]
    public string Status { get; set; } = string.Empty;
}
