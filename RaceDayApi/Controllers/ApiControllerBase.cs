using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace RaceDayApi.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
