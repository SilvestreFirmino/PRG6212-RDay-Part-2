using Microsoft.AspNetCore.Mvc;
using RaceDayApi.Controllers;
using RaceDayApi.Models;
using RaceDayApi.Services;

namespace RaceDayApi.Tests;

public class AuthControllerTests
{
    private static AuthController Controller()
    {
        var controller = new AuthController(TestHelpers.Database(), new PasswordService());
        TestHelpers.SetUser(controller, 0, string.Empty);
        return controller;
    }

    [Fact]
    public async Task Participant_registration_and_login_work()
    {
        var controller = Controller();
        var request = new User { Email = "participant@test.co.za", Password = "Password123!", FirstName = "Nomsa", LastName = "Mthembu", Role = Roles.Participant, ParticipantProfile = new ParticipantProfile { DateOfBirth = new DateTime(1995, 1, 1), Gender = "Female", EmergencyContactName = "Sipho", EmergencyContactPhone = "0712345678" } };
        var register = await controller.Register(request);
        Assert.IsType<OkObjectResult>(register);
        var login = await controller.Login(new LoginDetails { Email = request.Email, Password = "Password123!" });
        Assert.NotNull(Assert.IsType<OkObjectResult>(login).Value);
    }

    [Fact]
    public async Task Incorrect_password_is_rejected()
    {
        var controller = Controller();
        await controller.Register(new User { Email = "organiser@test.co.za", Password = "Password123!", FirstName = "Lerato", LastName = "Mokoena", Role = Roles.Organiser });
        var result = await controller.Login(new LoginDetails { Email = "organiser@test.co.za", Password = "wrong-password" });
        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}
