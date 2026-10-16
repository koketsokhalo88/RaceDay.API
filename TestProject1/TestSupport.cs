using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Api.Tests;

public class RaceDayFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(s => s.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName)));
    }
}

public static class Api
{
    public const string Pwd = "Password123!";

    public static async Task<HttpClient> LoggedIn(RaceDayFactory f, string role, string? email = null)
    {
        email ??= $"{Guid.NewGuid():N}@test.com";
        var client = f.CreateClient();   // each client has its own cookie jar (its own session)
        (await client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Test User", email, password = Pwd, role })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/auth/login", new { email, password = Pwd })).EnsureSuccessStatusCode();
        return client;
    }

    public static async Task<int> ReadId(HttpResponseMessage r, string prop)
    {
        var json = await r.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty(prop).GetInt32();
    }

    public static object EventBody(string name = "City 10K") => new
    { eventName = name, eventDate = "2026-12-01", location = "Cape Town", description = "Test event", eventType = "Run", distanceKm = 10 };

    public static async Task<int> CreateEvent(HttpClient organiser)
        => await ReadId(await organiser.PostAsJsonAsync("/api/events", EventBody()), "eventId");

    public static async Task<int> CreateCategory(HttpClient organiser, int eventId, int max = 100)
        => await ReadId(await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { categoryName = "10km", distanceKm = 10, maxParticipants = max }), "categoryId");

    public static async Task<int> Enrol(HttpClient participant, int categoryId)
        => await ReadId(await participant.PostAsync($"/api/categories/{categoryId}/enrolments", null), "enrolmentId");
}
