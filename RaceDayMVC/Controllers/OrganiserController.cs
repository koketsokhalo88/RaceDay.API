using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Filters;
using RaceDayMVC.Services;
using RaceDayMVC.ViewModels;

namespace RaceDayMVC.Controllers;

[RequireRole("Organiser")]
public class OrganiserController : Controller
{
    private readonly RaceDayApiClient _api;
    public OrganiserController(RaceDayApiClient api) => _api = api;

    private int MyId() => HttpContext.Session.GetInt32("userId")!.Value;

    // Dashboard: my events + enrolment counts + upcoming dates
    public async Task<IActionResult> Dashboard()
    {
        var mine = (await _api.GetEventsAsync() ?? new()).Where(e => e.OrganiserId == MyId()).ToList();
        var rows = new List<DashboardRow>();
        foreach (var e in mine)
        {
            var cats = await _api.GetCategoriesAsync(e.Id) ?? new();
            var total = 0;
            foreach (var c in cats) total += (await _api.CategoryEnrolmentsAsync(c.Id))?.Count ?? 0;
            rows.Add(new DashboardRow(e, total));
        }
        return View(rows);
    }

    public IActionResult CreateEvent() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventFormVm vm)
    {
        var (ok, id, error) = await _api.CreateEventAsync(vm);
        if (!ok) { ViewBag.Error = error; return View(vm); }
        // Optional banner right after creating
        if (Request.Form.Files.Count > 0)
            await _api.UploadBannerAsync(id!.Value, Request.Form.Files[0]);
        return RedirectToAction(nameof(Dashboard));
    }

    public async Task<IActionResult> EditEvent(int id)
    {
        var ev = await _api.GetEventAsync(id);
        if (ev == null || ev.OrganiserId != MyId()) return Forbid();
        return View(new EventFormVm { EventName = ev.EventName, EventDate = ev.EventDate, Location = ev.Location, Description = ev.Description, EventType = ev.EventType });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(int id, EventFormVm vm)
    {
        var (ok, error) = await _api.UpdateEventAsync(id, vm);
        if (!ok) { ViewBag.Error = error; return View(vm); }
        if (Request.Form.Files.Count > 0)
            await _api.UploadBannerAsync(id, Request.Form.Files[0]);
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    { await _api.DeleteEventAsync(id); return RedirectToAction(nameof(Dashboard)); }

    // Category management
    public async Task<IActionResult> Categories(int eventId)
    {
        ViewBag.EventId = eventId;
        return View(await _api.GetCategoriesAsync(eventId) ?? new());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(int eventId, CategoryFormVm vm)
    {
        var (ok, error) = await _api.CreateCategoryAsync(eventId, vm);
        if (!ok) TempData["Error"] = error;
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    // Enrolments per event
    public async Task<IActionResult> Enrolments(int eventId)
    {
        var ev = await _api.GetEventAsync(eventId);
        if (ev == null || ev.OrganiserId != MyId()) return Forbid();
        var cats = await _api.GetCategoriesAsync(eventId) ?? new();
        var rows = new List<EnrolmentVm>();
        foreach (var c in cats) rows.AddRange(await _api.CategoryEnrolmentsAsync(c.Id) ?? new());
        ViewBag.Event = ev;
        return View(rows);
    }

    // Results entry
    public async Task<IActionResult> Results(int eventId) => await Enrolments(eventId); // reuse list, different view

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddResult(int eventId, int enrolmentId, string finishTime, int? position, string status)
    {
        TimeSpan? ft = TimeSpan.TryParse(finishTime, out var t) ? t : null;
        var (ok, error) = await _api.AddResultAsync(enrolmentId, new ResultFormVm { FinishTime = ft, Position = position, Status = status });
        if (!ok) TempData["Error"] = error;
        return RedirectToAction(nameof(Results), new { eventId });
    }

    public record DashboardRow(EventVm Event, int TotalEnrolments);
}
