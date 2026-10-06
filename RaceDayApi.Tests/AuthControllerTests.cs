using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RaceDayApi.Controllers;
using RaceDayApi.DTOs;
using RaceDayApi.Models;
using RaceDayApi.Services;

namespace RaceDayApi.Tests;

public class AuthControllerTests
{
    private static AuthController Controller()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "ADevelopmentTestKeyThatIsLongEnoughForJwtSigning123!", ["Jwt:Issuer"] = "RaceDayApi", ["Jwt:Audience"] = "RaceDayApiClient"
        }).Build();
        return new AuthController(TestHelpers.Database(), new PasswordService(), new JwtTokenService(configuration));
    }

    [Fact]
    public async Task Participant_registration_and_login_return_a_token()
    {
        var controller = Controller();
        var request = new RegisterRequest { Email = "participant@test.co.za", Password = "Password123!", FirstName = "Nomsa", LastName = "Mthembu", Role = Roles.Participant, ParticipantProfile = new ParticipantProfileRequest { DateOfBirth = new DateTime(1995, 1, 1), Gender = "Female", EmergencyContactName = "Sipho", EmergencyContactPhone = "0712345678" } };
        var register = await controller.Register(request);
        var created = Assert.IsType<CreatedAtActionResult>(register.Result);
        Assert.IsType<AuthResponse>(created.Value);
        var login = await controller.Login(new LoginRequest { Email = request.Email, Password = request.Password });
        Assert.NotNull(Assert.IsType<OkObjectResult>(login.Result).Value);
    }

    [Fact]
    public async Task Incorrect_password_is_rejected()
    {
        var controller = Controller();
        await controller.Register(new RegisterRequest { Email = "organiser@test.co.za", Password = "Password123!", FirstName = "Lerato", LastName = "Mokoena", Role = Roles.Organiser });
        var result = await controller.Login(new LoginRequest { Email = "organiser@test.co.za", Password = "wrong-password" });
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }
}
