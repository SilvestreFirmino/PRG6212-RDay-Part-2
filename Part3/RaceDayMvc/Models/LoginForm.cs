using System.ComponentModel.DataAnnotations;

namespace RaceDayMvc.Models;

public class LoginForm
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
}

public class Profile
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string Role { get; set; } = "";
}
