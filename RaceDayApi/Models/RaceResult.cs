using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaceDayApi.Models;

public class RaceResult
{
    public int ResultId { get; set; }
    public int EnrollmentId { get; set; }
    public int RecordedByOrganiserId { get; set; }
    [Required, MaxLength(10)] public string ResultStatus { get; set; } = string.Empty;
    public long? DurationMilliseconds { get; set; }
    public int? OverallPosition { get; set; }
    public int? CategoryPosition { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    [JsonIgnore]
    public EventEnrollment? Enrollment { get; set; }

    [JsonIgnore]
    public User? RecordedByOrganiser { get; set; }
}
