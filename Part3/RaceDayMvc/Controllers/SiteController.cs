using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDayMvc.Models;
using RaceDayMvc.Services;

namespace RaceDayMvc.Controllers;

public abstract class SiteController : Controller
{
    protected readonly RaceDayClient Api;
    protected virtual string? RequiredRole => null;
    protected SiteController(RaceDayClient api) { Api = api; }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        try
        {
            if (RequiredRole != null)
            {
                if (HttpContext.Session.GetString("ApiCookie") == null)
                { context.Result = RedirectToAction("Login", "Account"); return; }
                var profile = await Api.Get<Profile>("api/profile");
                HttpContext.Session.SetString("Role", profile.Role);
                if (RequiredRole != "Any" && profile.Role != RequiredRole)
                { context.Result = View("Problem", "This page is for " + RequiredRole + " accounts."); return; }
            }
            var result = await next();
            if (result.Exception is ApiException error)
            {
                result.ExceptionHandled = true;
                result.Result = error.Status == 401 ? RedirectToAction("Login", "Account") : View("Problem", error.Message);
            }
        }
        catch (ApiException error)
        {
            context.Result = error.Status == 401 ? RedirectToAction("Login", "Account") : View("Problem", error.Message);
        }
    }
    protected IEnumerable<KeyValuePair<string,string>> Fields() => Request.Form
        .Where(f => f.Key != "__RequestVerificationToken")
        .Select(f => new KeyValuePair<string,string>(f.Key, f.Value[0] ?? ""));
    protected async Task<bool> Save(HttpMethod method, string path)
    {
        try { await Api.Form(method, path, Fields()); return true; }
        catch (ApiException error) { ModelState.AddModelError("", error.Message); return false; }
    }
}
