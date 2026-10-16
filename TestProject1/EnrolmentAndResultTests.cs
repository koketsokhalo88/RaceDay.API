using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Api.Tests;

public class EnrolmentAndResultTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _f;
    public EnrolmentAndResultTests(RaceDayFactory f) => _f = f;

    private async Task<(HttpClient org, int eventId, int catId)> Setup(int max = 100)
    {
        var org = await Api.LoggedIn(_f, "Organiser");
        var eventId = await Api.CreateEvent(org);
        var catId = await Api.CreateCategory(org, eventId, max);
        return (org, eventId, catId);
    }

    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsRecorded()
    {
        var (_, _, catId) = await Setup();
        var p = await Api.LoggedIn(_f, "Participant");
        var r = await p.PostAsync($"/api/categories/{catId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);

        var mine = await p.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/users/me/enrolments");
        Assert.Equal(1, mine.GetArrayLength());
    }

    [Fact]
    public async Task Participant_EnrolTwice_Returns409()
    {
        var (_, _, catId) = await Setup();
        var p = await Api.LoggedIn(_f, "Participant");
        await p.PostAsync($"/api/categories/{catId}/enrolments", null);
        var r = await p.PostAsync($"/api/categories/{catId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Enrol_WhenCategoryFull_Returns409()
    {
        var (_, _, catId) = await Setup(max: 1);
        var p1 = await Api.LoggedIn(_f, "Participant");
        var p2 = await Api.LoggedIn(_f, "Participant");
        await p1.PostAsync($"/api/categories/{catId}/enrolments", null);
        var r = await p2.PostAsync($"/api/categories/{catId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Enrol_MissingCategory_Returns404()
    {
        var p = await Api.LoggedIn(_f, "Participant");
        Assert.Equal(HttpStatusCode.NotFound, (await p.PostAsync("/api/categories/99999/enrolments", null)).StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrol_Returns403()
    {
        var (org, _, catId) = await Setup();
        Assert.Equal(HttpStatusCode.Forbidden, (await org.PostAsync($"/api/categories/{catId}/enrolments", null)).StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_Enrol_Returns401()
    {
        var (_, _, catId) = await Setup();
        var r = await _f.CreateClient().PostAsync($"/api/categories/{catId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewEnrolments_Participant_Cannot()
    {
        var (org, _, catId) = await Setup();
        var p = await Api.LoggedIn(_f, "Participant");
        await p.PostAsync($"/api/categories/{catId}/enrolments", null);
        Assert.Equal(HttpStatusCode.OK, (await org.GetAsync($"/api/categories/{catId}/enrolments")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await p.GetAsync($"/api/categories/{catId}/enrolments")).StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotViewMyEnrolments_Returns403()
    {
        var org = await Api.LoggedIn(_f, "Organiser");
        Assert.Equal(HttpStatusCode.Forbidden, (await org.GetAsync("/api/users/me/enrolments")).StatusCode);
    }

    [Fact]
    public async Task Participant_CanCancelOwn_OtherParticipantCannot()
    {
        var (_, _, catId) = await Setup();
        var owner = await Api.LoggedIn(_f, "Participant");
        var other = await Api.LoggedIn(_f, "Participant");
        var enrolId = await Api.Enrol(owner, catId);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/api/enrolments/{enrolId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/enrolments/{enrolId}")).StatusCode);
    }

    [Fact]
    public async Task Organiser_CanRecordResult_Returns201()
    {
        var (org, _, catId) = await Setup();
        var p = await Api.LoggedIn(_f, "Participant");
        var enrolId = await Api.Enrol(p, catId);
        var r = await org.PostAsJsonAsync($"/api/enrolments/{enrolId}/result",
            new { finishTime = "00:52:14", position = 3, status = "Finished" });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotRecordResult_Returns403()
    {
        var (_, _, catId) = await Setup();
        var p = await Api.LoggedIn(_f, "Participant");
        var enrolId = await Api.Enrol(p, catId);
        var r = await p.PostAsJsonAsync($"/api/enrolments/{enrolId}/result",
            new { finishTime = "00:50:00", position = 1, status = "Finished" });
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task RecordResult_Twice_Returns409()
    {
        var (org, _, catId) = await Setup();
        var p = await Api.LoggedIn(_f, "Participant");
        var enrolId = await Api.Enrol(p, catId);
        var body = new { finishTime = "00:52:14", position = 3, status = "Finished" };
        await org.PostAsJsonAsync($"/api/enrolments/{enrolId}/result", body);
        var r = await org.PostAsJsonAsync($"/api/enrolments/{enrolId}/result", body);
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Participant_ViewsOwnResult_OtherParticipantRejected()
    {
        var (org, _, catId) = await Setup();
        var owner = await Api.LoggedIn(_f, "Participant");
        var other = await Api.LoggedIn(_f, "Participant");
        var enrolId = await Api.Enrol(owner, catId);
        await org.PostAsJsonAsync($"/api/enrolments/{enrolId}/result",
            new { finishTime = "01:10:00", position = 5, status = "Finished" });
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/enrolments/{enrolId}/result")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/enrolments/{enrolId}/result")).StatusCode);
    }

    [Fact]
    public async Task Leaderboard_IsPublic_Returns200()
    {
        var (_, _, catId) = await Setup();
        Assert.Equal(HttpStatusCode.OK, (await _f.CreateClient().GetAsync($"/api/categories/{catId}/results")).StatusCode);
    }
}
