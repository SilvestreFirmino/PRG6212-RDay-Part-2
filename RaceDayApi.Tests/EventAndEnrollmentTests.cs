using Microsoft.AspNetCore.Mvc;
using RaceDayApi.Controllers;
using RaceDayApi.Models;

namespace RaceDayApi.Tests;

public class EventAndEnrollmentTests
{
    [Fact]
    public void Organiser_can_create_an_event()
    {
        using var db = TestHelpers.Database();
        db.Users.Add(new User { UserId = 1, Email = "organiser@test.co.za", PasswordHash = "test", FirstName = "Lerato", LastName = "Mokoena", Role = Roles.Organiser }); db.SaveChanges();
        var controller = new EventsController(db); TestHelpers.SetUser(controller, 1, Roles.Organiser);
        var request = new RaceEvent { Name = "Jozi Run", Description = "Road race", EventType = "Running", StartDateTime = DateTime.UtcNow.AddDays(30), EndDateTime = DateTime.UtcNow.AddDays(30).AddHours(3), VenueName = "Park", AddressLine1 = "1 Main Road", City = "Johannesburg", Province = "Gauteng", RegistrationOpenUtc = DateTime.UtcNow, RegistrationCloseUtc = DateTime.UtcNow.AddDays(20) };
        var result = controller.CreateEvent(request);
        Assert.IsType<OkObjectResult>(result);
        Assert.Single(db.Events);
    }

    [Fact]
    public void Event_management_rejects_a_participant()
    {
        using var db = TestHelpers.Database();
        var controller = new EventsController(db); TestHelpers.SetUser(controller, 2, Roles.Participant);
        var request = new RaceEvent { Name = "Test", Description = "Test", EventType = "Running", StartDateTime = DateTime.UtcNow.AddDays(1), EndDateTime = DateTime.UtcNow.AddDays(2), VenueName = "Park", AddressLine1 = "Road", City = "Cape Town", Province = "Western Cape", RegistrationOpenUtc = DateTime.UtcNow, RegistrationCloseUtc = DateTime.UtcNow.AddHours(1) };
        var result = controller.CreateEvent(request);
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void Participant_can_enrol_once_in_an_open_category()
    {
        using var db = TestHelpers.Database();
        var organiser = new User { UserId = 1, Email = "o@test.co.za", PasswordHash = "x", FirstName = "O", LastName = "One", Role = Roles.Organiser };
        var participant = new User { UserId = 2, Email = "p@test.co.za", PasswordHash = "x", FirstName = "P", LastName = "One", Role = Roles.Participant };
        var race = new RaceEvent { EventId = 1, OrganiserId = 1, Name = "Cape Run", Description = "Run", EventType = "Running", StartDateTime = DateTime.UtcNow.AddDays(7), EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(3), VenueName = "Park", AddressLine1 = "Road", City = "Cape Town", Province = "Western Cape", RegistrationOpenUtc = DateTime.UtcNow.AddDays(-1), RegistrationCloseUtc = DateTime.UtcNow.AddDays(5), Status = "Published" };
        var category = new Category { CategoryId = 1, EventId = 1, Name = "10km", DistanceKm = 10, EntryFee = 200, Capacity = 100, IsActive = true };
        db.AddRange(organiser, participant, race, category); db.SaveChanges();
        var controller = new EnrollmentsController(db); TestHelpers.SetUser(controller, 2, Roles.Participant);
        var result = controller.Enrol(1, new EventEnrollment { CategoryId = 1, EmergencyConsent = true });
        Assert.IsType<OkObjectResult>(result);
        Assert.Single(db.EventEnrollments);
    }
}
