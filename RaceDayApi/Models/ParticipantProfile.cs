using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.Models;

public class ParticipantProfile
{
    public int ParticipantProfileId { get; set; }
    public int UserId { get; set; }
    [DataType(DataType.Date)] public DateTime DateOfBirth { get; set; }
    [Required, MaxLength(20)] public string Gender { get; set; } = string.Empty;
    [Required, MaxLength(160)] public string EmergencyContactName { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string EmergencyContactPhone { get; set; } = string.Empty;
    [MaxLength(500)] public string? MedicalNotes { get; set; }
    [MaxLength(120)] public string? ClubName { get; set; }
    public User? User { get; set; }
}
