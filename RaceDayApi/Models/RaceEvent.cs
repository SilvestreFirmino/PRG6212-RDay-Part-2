using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.Models;

public class RaceEvent
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    [Required, MaxLength(160)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(2000)] public string Description { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string EventType { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    [Required, MaxLength(64)] public string TimeZoneId { get; set; } = "South Africa Standard Time";
    [Required, MaxLength(160)] public string VenueName { get; set; } = string.Empty;
    [Required, MaxLength(160)] public string AddressLine1 { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string City { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Province { get; set; } = string.Empty;
    [MaxLength(10)] public string? PostalCode { get; set; }
    public DateTime RegistrationOpenUtc { get; set; }
    public DateTime RegistrationCloseUtc { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "Draft";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public User? Organiser { get; set; }
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<EventEnrollment> Enrollments { get; set; } = new List<EventEnrollment>();
}
