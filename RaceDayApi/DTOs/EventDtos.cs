using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.DTOs;

public class EventRequest
{
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
}

public class EventStatusRequest { [Required] public string Status { get; set; } = string.Empty; }

public class CategoryRequest
{
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    [Range(0.01, 1000)] public decimal DistanceKm { get; set; }
    [Range(0, 100000)] public decimal EntryFee { get; set; }
    [Range(1, 1000000)] public int Capacity { get; set; }
    [Range(0, 120)] public int? MinimumAge { get; set; }
    [Range(0, 120)] public int? MaximumAge { get; set; }
    public TimeSpan? CategoryStartTime { get; set; }
    public bool IsActive { get; set; } = true;
}
