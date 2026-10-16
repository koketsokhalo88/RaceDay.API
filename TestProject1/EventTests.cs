using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Api.Tests;

public class EventTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _f;
    public EventTests(RaceDayFactory f) => _f = f;

    [Fact]
    public async Task Organiser_CanCreateEvent_Returns201()
    {
        var org = await Api.LoggedIn(_f, "Organiser");
        var r = await org.PostAsJsonAsync("/api/events", Api.EventBody());
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    [Fact]
    public async Task Organiser_InvalidEventType_Returns400()
    {
        var org = await Api.LoggedIn(_f, "Organiser");
        var r = await org.PostAsJsonAsync("/api/events",
            new { eventName = "X", eventDate = "2026-12-01", location = "Y", description = "", eventType = "Swim", distanceKm = 5 });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Participant_CreateEvent_Returns403()
    {
        var p = await Api.LoggedIn(_f, "Participant");
        var r = await p.PostAsJsonAsync("/api/events", Api.EventBody());
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_CreateEvent_Returns401()
    {
        var r = await _f.CreateClient().PostAsJsonAsync("/api/events", Api.EventBody());
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Anyone_CanListEvents_Returns200()
    {
        var r = await _f.CreateClient().GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent_Returns200()
    {
        var org = await Api.LoggedIn(_f, "Organiser");
        var id = await Api.CreateEvent(org);
        var r = await org.PutAsJsonAsync($"/api/events/{id}", Api.EventBody("Renamed Event"));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent_Returns403()
    {
        var owner = await Api.LoggedIn(_f, "Organiser");
        var other = await Api.LoggedIn(_f, "Organiser");
        var id = await Api.CreateEvent(owner);
        var r = await other.PutAsJsonAsync($"/api/events/{id}", Api.EventBody("Hijack"));
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Organiser_UpdateMissingEvent_Returns404()
    {
        var org = await Api.LoggedIn(_f, "Organiser");
        var r = await org.PutAsJsonAsync("/api/events/99999", Api.EventBody());
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent_Returns204_ThenGone()
    {
        var org = await Api.LoggedIn(_f, "Organiser");
        var id = await Api.CreateEvent(org);
        Assert.Equal(HttpStatusCode.NoContent, (await org.DeleteAsync($"/api/events/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await org.GetAsync($"/api/events/{id}")).StatusCode);
    }

    [Fact]
    public async Task Participant_DeleteEvent_Returns403()
    {
        var org = await Api.LoggedIn(_f, "Organiser");
        var p = await Api.LoggedIn(_f, "Participant");
        var id = await Api.CreateEvent(org);
        Assert.Equal(HttpStatusCode.Forbidden, (await p.DeleteAsync($"/api/events/{id}")).StatusCode);
    }

    [Fact]
    public async Task Organiser_CanAddCategory_Participant_Cannot()
    {
        var org = await Api.LoggedIn(_f, "Organiser");
        var p = await Api.LoggedIn(_f, "Participant");
        var id = await Api.CreateEvent(org);
        var body = new { categoryName = "5km", distanceKm = 5, maxParticipants = 50 };
        Assert.Equal(HttpStatusCode.Created, (await org.PostAsJsonAsync($"/api/events/{id}/categories", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await p.PostAsJsonAsync($"/api/events/{id}/categories", body)).StatusCode);
    }
}