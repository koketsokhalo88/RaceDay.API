using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Api.Tests;

public class AuthTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _f;
    public AuthTests(RaceDayFactory f) => _f = f;

    [Fact]
    public async Task Register_ValidParticipant_Returns201()
    {
        var r = await _f.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { fullName = "Liam Botha", email = "liam@test.com", password = Api.Pwd, role = "Participant" });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var c = _f.CreateClient();
        var body = new { fullName = "Dup", email = "dup@test.com", password = Api.Pwd, role = "Participant" };
        await c.PostAsJsonAsync("/api/auth/register", body);
        var r = await c.PostAsJsonAsync("/api/auth/register", body);
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var r = await _f.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { fullName = "Bad", email = "badrole@test.com", password = Api.Pwd, role = "Admin" });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var r = await _f.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { fullName = "Short", email = "short@test.com", password = "123", role = "Participant" });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotPlainText()
    {
        var email = "hash@test.com";
        await Api.LoggedIn(_f, "Participant", email);
        using var scope = _f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = db.Users.Single(u => u.Email == email);
        Assert.NotEqual(Api.Pwd, user.PasswordHash);
        Assert.StartsWith("$2", user.PasswordHash);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200()
    {
        var c = _f.CreateClient();
        await c.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Login", email = "login@test.com", password = Api.Pwd, role = "Organiser" });
        var r = await c.PostAsJsonAsync("/api/auth/login", new { email = "login@test.com", password = Api.Pwd });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var c = _f.CreateClient();
        await c.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Wrong", email = "wrong@test.com", password = Api.Pwd, role = "Organiser" });
        var r = await c.PostAsJsonAsync("/api/auth/login", new { email = "wrong@test.com", password = "NotTheRightOne1" });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Profile_NotLoggedIn_Returns401()
    {
        var r = await _f.CreateClient().GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Profile_LoggedIn_Returns200()
    {
        var c = await Api.LoggedIn(_f, "Participant");
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/users/me")).StatusCode);
    }

    [Fact]
    public async Task Profile_Update_Returns200()
    {
        var c = await Api.LoggedIn(_f, "Participant");
        var r = await c.PutAsJsonAsync("/api/users/me", new { fullName = "New Name", email = "newname@test.com" });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsSession()
    {
        var c = await Api.LoggedIn(_f, "Participant");
        await c.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/users/me")).StatusCode);
    }
}
