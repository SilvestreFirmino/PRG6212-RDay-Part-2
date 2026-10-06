using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.DTOs;

public class EnrollmentRequest
{
    public int CategoryId { get; set; }
    public bool EmergencyConsent { get; set; }
}

public class EnrollmentUpdateRequest
{
    [MaxLength(20)] public string? BibNumber { get; set; }
    [MaxLength(20)] public string? Status { get; set; }
    [MaxLength(20)] public string? PaymentStatus { get; set; }
}

public class ResultRequest
{
    [Required, MaxLength(10)] public string ResultStatus { get; set; } = string.Empty;
    public long? DurationMilliseconds { get; set; }
    public int? OverallPosition { get; set; }
    public int? CategoryPosition { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
}
