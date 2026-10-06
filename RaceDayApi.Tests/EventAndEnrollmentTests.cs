using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDayApi.Controllers;
using RaceDayApi.DTOs;
using RaceDayApi.Models;

namespace RaceDayApi.Tests;

public class EventAndEnrollmentTests
{
    [Fact]
    public async Task Organiser_can_create_an_event()
    {
        await using var db = TestHelpers.Database();
        db.Users.Add(new User { UserId = 1, Email = "organiser@test.co.za", PasswordHash = "test", FirstName = "Lerato", LastName = "Mokoena", Role = Roles.Organiser }); await db.SaveChangesAsync();
        var controller = new EventsController(db); TestHelpers.SetUser(controller, 1, Roles.Organiser);
        var request = new EventRequest { Name = "Jozi Run", Description = "Road race", EventType = "Running", StartDateTime = DateTime.UtcNow.AddDays(30), EndDateTime = DateTime.UtcNow.AddDays(30).AddHours(3), VenueName = "Park", AddressLine1 = "1 Main Road", City = "Johannesburg", Province = "Gauteng", RegistrationOpenUtc = DateTime.UtcNow, RegistrationCloseUtc = DateTime.UtcNow.AddDays(20) };
        var result = await controller.Create(request);
        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Single(db.Events);
    }

    [Fact]
    public void Event_management_requires_organiser_role()
    {
        var attribute = typeof(EventsController).GetMethod(nameof(EventsController.Create))!.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single();
        Assert.Equal(Roles.Organiser, attribute.Roles);
    }

    [Fact]
    public async Task Participant_can_enrol_once_in_an_open_category()
    {
        await using var db = TestHelpers.Database();
        var organiser = new User { UserId = 1, Email = "o@test.co.za", PasswordHash = "x", FirstName = "O", LastName = "One", Role = Roles.Organiser };
        var participant = new User { UserId = 2, Email = "p@test.co.za", PasswordHash = "x", FirstName = "P", LastName = "One", Role = Roles.Participant };
        var race = new RaceEvent { EventId = 1, OrganiserId = 1, Name = "Cape Run", Description = "Run", EventType = "Running", StartDateTime = DateTime.UtcNow.AddDays(7), EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(3), VenueName = "Park", AddressLine1 = "Road", City = "Cape Town", Province = "Western Cape", RegistrationOpenUtc = DateTime.UtcNow.AddDays(-1), RegistrationCloseUtc = DateTime.UtcNow.AddDays(5), Status = "Published" };
        var category = new Category { CategoryId = 1, EventId = 1, Name = "10km", DistanceKm = 10, EntryFee = 200, Capacity = 100, IsActive = true };
        db.AddRange(organiser, participant, race, category); await db.SaveChangesAsync();
        var controller = new EnrollmentsController(db); TestHelpers.SetUser(controller, 2, Roles.Participant);
        var result = await controller.Enrol(1, new EnrollmentRequest { CategoryId = 1, EmergencyConsent = true });
        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Single(db.EventEnrollments);
    }
}
