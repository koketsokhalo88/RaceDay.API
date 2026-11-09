using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Services;

namespace RaceDayMVC.Controllers;

public class AccountController : Controller
{
    private readonly RaceDayApiClient _api;
    public AccountController(RaceDayApiClient api) => _api = api;

    public IActionResult Login() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password)
    {
        var (ok, token, role, userId, error) = await _api.LoginAsync(email, password);
        if (!ok) { ViewBag.Error = error; return View(); }
        HttpContext.Session.SetString("token", token!);
        HttpContext.Session.SetString("role", role!);
        HttpContext.Session.SetInt32("userId", userId);
        HttpContext.Session.SetString("name", email);
        return role == "Organiser" ? RedirectToAction("Dashboard", "Organiser")
                                   : RedirectToAction("Index", "Home");
    }

    public IActionResult Register() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string fullName, string email, string password, string role)
    {
        var (ok, error) = await _api.RegisterAsync(fullName, email, password, role);
        if (!ok) { ViewBag.Error = error; return View(); }
        return RedirectToAction(nameof(Login));
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}
