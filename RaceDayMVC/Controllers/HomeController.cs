using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Services;

namespace RaceDayMVC.Controllers;

public class HomeController : Controller
{
    private readonly RaceDayApiClient _api;
    public HomeController(RaceDayApiClient api) => _api = api;

    // Public: upcoming events with filtering
    public async Task<IActionResult> Index(string? search, string? type, string? location)
    {
        var events = (await _api.GetEventsAsync()) ?? new();
        var q = events.Where(e => e.EventDate >= DateTime.Today);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(e => e.EventName.Contains(search, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(type)) q = q.Where(e => e.EventType == type);
        if (!string.IsNullOrWhiteSpace(location)) q = q.Where(e => e.Location.Contains(location, StringComparison.OrdinalIgnoreCase));
        ViewBag.Search = search; ViewBag.Type = type; ViewBag.Location = location;
        return View(q.OrderBy(e => e.EventDate).ToList());
    }

    public async Task<IActionResult> Details(int id)
    {
        var ev = await _api.GetEventAsync(id);
        if (ev == null) return NotFound();
        ViewBag.Role = HttpContext.Session.GetString("role");
        return View(ev);
    }
}