using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.Models;

public class User
{
    public int UserId { get; set; }
    [Required, MaxLength(254)] public string Email { get; set; } = string.Empty;
    [Required, MaxLength(512)] public string PasswordHash { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string FirstName { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string LastName { get; set; } = string.Empty;
    [MaxLength(20)] public string? PhoneNumber { get; set; }
    [Required, MaxLength(20)] public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public ParticipantProfile? ParticipantProfile { get; set; }
    public ICollection<RaceEvent> OrganisedEvents { get; set; } = new List<RaceEvent>();
    public ICollection<EventEnrollment> Enrollments { get; set; } = new List<EventEnrollment>();
    public ICollection<RaceResult> RecordedResults { get; set; } = new List<RaceResult>();
}
