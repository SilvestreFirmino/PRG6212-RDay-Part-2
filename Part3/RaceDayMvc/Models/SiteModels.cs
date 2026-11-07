using System.ComponentModel.DataAnnotations;

namespace RaceDayMvc.Models;

// View models describe the fields and data shown on pages. They do not access SQL Server.
public class EventPage
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    [Required, MaxLength(160)] public string Name { get; set; } = "";
    [Required, MaxLength(2000)] public string Description { get; set; } = "";
    [Required] public string EventType { get; set; } = "Running";
    public DateTime StartDateTime { get; set; } = DateTime.Today.AddDays(30).AddHours(7);
    public DateTime EndDateTime { get; set; } = DateTime.Today.AddDays(30).AddHours(12);
    [Required] public string VenueName { get; set; } = "";
    [Required] public string AddressLine1 { get; set; } = "";
    [Required] public string City { get; set; } = "";
    [Required] public string Province { get; set; } = "";
    public string? PostalCode { get; set; }
    public string TimeZoneId { get; set; } = "South Africa Standard Time";
    public DateTime RegistrationOpenUtc { get; set; } = DateTime.UtcNow;
    public DateTime RegistrationCloseUtc { get; set; } = DateTime.UtcNow.AddDays(20);
    public string Status { get; set; } = "Draft";
    public string? BannerUrl { get; set; }
    public int EnrolmentCount { get; set; }
    public List<CategoryPage> Categories { get; set; } = new();
}
public class CategoryPage
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    [Required] public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public int Capacity { get; set; } = 100;
    public int? MinimumAge { get; set; }
    public int? MaximumAge { get; set; }
    public string? CategoryStartTime { get; set; }
    public bool IsActive { get; set; } = true;
}
public class EntryPage
{
    public int EnrollmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public string ParticipantName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string Status { get; set; } = "";
    public decimal FeePaid { get; set; }
    public DateTime StartDateTime { get; set; }
    public int? ResultId { get; set; }
    public string? ResultStatus { get; set; }
    public long? DurationMilliseconds { get; set; }
    public int? OverallPosition { get; set; }
    public int? CategoryPosition { get; set; }
    public string? Notes { get; set; }
    public string FinishTime => DurationMilliseconds.HasValue
        ? TimeSpan.FromMilliseconds(DurationMilliseconds.Value).ToString(@"hh\:mm\:ss") : "—";
}
public class RegisterPage
{
    [Required, MaxLength(80)] public string FirstName { get; set; } = "";
    [Required, MaxLength(80)] public string LastName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(8), DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required] public string Role { get; set; } = "Participant";
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
}
public class ProfilePage
{
    [Required] public string FirstName { get; set; } = "";
    [Required] public string LastName { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string? Role { get; set; }
    public ParticipantDetails? ParticipantProfile { get; set; }
}
public class ParticipantDetails
{
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? MedicalNotes { get; set; }
    public string? ClubName { get; set; }
}
