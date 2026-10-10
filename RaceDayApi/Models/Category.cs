using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaceDayApi.Models;

public class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public decimal DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public int Capacity { get; set; }
    public int? MinimumAge { get; set; }
    public int? MaximumAge { get; set; }
    public TimeSpan? CategoryStartTime { get; set; }
    public bool IsActive { get; set; } = true;
    [JsonIgnore]
    public RaceEvent? Event { get; set; }

    [JsonIgnore]
    public ICollection<EventEnrollment> Enrollments { get; set; } = new List<EventEnrollment>();
}
