using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaceDayApi.Models;

public class EventEnrollment
{
    public int EnrollmentId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public int ParticipantId { get; set; }
    [MaxLength(20)] public string? BibNumber { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "Pending";
    [Required, MaxLength(20)] public string PaymentStatus { get; set; } = "Unpaid";
    public decimal FeePaid { get; set; }
    public bool EmergencyConsent { get; set; }
    public DateTime EnrolledAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    [JsonIgnore]
    public RaceEvent? Event { get; set; }

    [JsonIgnore]
    public Category? Category { get; set; }

    [JsonIgnore]
    public User? Participant { get; set; }

    [JsonIgnore]
    public RaceResult? Result { get; set; }
}
