using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Filters;
using RaceDayMVC.Services;

namespace RaceDayMVC.Controllers;

[RequireRole("Participant")]
public class ParticipantController : Controller
{
    private readonly RaceDayApiClient _api;
    public ParticipantController(RaceDayApiClient api) => _api = api;

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        var (ok, error) = await _api.EnrolAsync(categoryId);
        TempData[ok ? "Success" : "Error"] = ok ? "Enrolment confirmed!" : error;
        return RedirectToAction("Details", "Home", new { id = eventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, int eventId)
    {
        var (ok, error) = await _api.CancelEnrolmentAsync(id);
        if (!ok) TempData["Error"] = error;
        return RedirectToAction(nameof(MyEnrolments));
    }

    public async Task<IActionResult> MyEnrolments()
    {
        var list = await _api.MyEnrolmentsAsync() ?? new();
        foreach (var e in list)   // fetch event info for display
            if (e.Category != null)
                e.Category.Event ??= await _api.GetEventAsync((await _api.GetCategoriesAsync(0))?.FirstOrDefault()?.EventId ?? 0); // see note below
        return View(list.OrderByDescending(e => e.EnrolledAt).ToList());
    }

    public async Task<IActionResult> MyResults()
    {
        var list = (await _api.MyEnrolmentsAsync() ?? new()).Where(e => e.Result != null).ToList();
        return View(list);
    }

    public async Task<IActionResult> Profile() => View(await _api.GetProfileAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(string fullName, string email)
    {
        var (ok, error) = await _api.UpdateProfileAsync(new() { FullName = fullName, Email = email });
        ViewBag.Message = ok ? "Profile updated." : error;
        return View(await _api.GetProfileAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPicture(IFormFile file)
    {
        var (ok, _, error) = await _api.UploadPictureAsync(file);
        TempData[ok ? "Success" : "Error"] = ok ? "Profile picture updated." : error;
        return RedirectToAction(nameof(Profile));
    }
}
