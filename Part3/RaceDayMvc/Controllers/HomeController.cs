using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Models;
using RaceDayMvc.Services;
namespace RaceDayMvc.Controllers;

public class HomeController : SiteController
{
    public HomeController(RaceDayClient api) : base(api) { }
    public async Task<IActionResult> Index(string? city, string? eventType)
    {
        ViewBag.City = city; ViewBag.EventType = eventType;
        var path = "api/events?upcoming=true&city=" + Uri.EscapeDataString(city ?? "") + "&eventType=" + Uri.EscapeDataString(eventType ?? "");
        return View(await Api.Get<List<EventPage>>(path));
    }
    public async Task<IActionResult> Details(int id)
    {
        var race = await Api.Get<EventPage>($"api/events/{id}");
        race.Categories = await Api.Get<List<CategoryPage>>($"api/events/{id}/categories");
        return View(race);
    }
    public async Task<IActionResult> Banner(int id)
    {
        try
        {
            using var response = await Api.Send(HttpMethod.Get, $"api/events/{id}/banner");
            return File(await response.Content.ReadAsByteArrayAsync(), response.Content.Headers.ContentType?.MediaType ?? "image/jpeg");
        }
        catch (ApiException) { return NotFound(); }
    }
    [IgnoreAntiforgeryToken]
    public IActionResult Error() => View("Problem", "Something went wrong. Please return home and try again.");
}
