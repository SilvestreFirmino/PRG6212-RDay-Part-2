using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RaceDayApi.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    // These helper methods keep session checks simple in every controller.
    protected int CurrentUserId => HttpContext.Session.GetInt32("UserId") ?? 0;
    protected string CurrentRole => HttpContext.Session.GetString("Role") ?? string.Empty;
    protected bool LoggedIn() => CurrentUserId > 0;
    protected bool IsOrganiser() => CurrentRole == "Organiser";
    protected bool IsParticipant() => CurrentRole == "Participant";
}
