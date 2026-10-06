using System.ComponentModel.DataAnnotations;

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
    public RaceEvent? Event { get; set; }
    public Category? Category { get; set; }
    public User? Participant { get; set; }
    public RaceResult? Result { get; set; }
}
