using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RaceDayApi.Models;

public class User
{
    public int UserId { get; set; }
    [Required, MaxLength(254)] public string Email { get; set; } = string.Empty;
    // The hashed password is saved in the database, but is never returned by the API.
    [JsonIgnore]
    [MaxLength(512)] public string PasswordHash { get; set; } = string.Empty;

    // Password is only used when a user registers or logs in. EF Core will not create a column for it.
    [NotMapped]
    public string Password { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string FirstName { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string LastName { get; set; } = string.Empty;
    [MaxLength(20)] public string? PhoneNumber { get; set; }
    [Required, MaxLength(20)] public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public ParticipantProfile? ParticipantProfile { get; set; }
    [JsonIgnore]
    public ICollection<RaceEvent> OrganisedEvents { get; set; } = new List<RaceEvent>();

    [JsonIgnore]
    public ICollection<EventEnrollment> Enrollments { get; set; } = new List<EventEnrollment>();

    [JsonIgnore]
    public ICollection<RaceResult> RecordedResults { get; set; } = new List<RaceResult>();
}
