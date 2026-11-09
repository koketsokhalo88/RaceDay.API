using RaceDayMVC.ViewModels;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDayMVC.Services;

public class RaceDayApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _acc;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public RaceDayApiClient(HttpClient http, IHttpContextAccessor acc) { _http = http; _acc = acc; }

    private void Auth()
    {
        var token = _acc.HttpContext?.Session.GetString("token");
        _http.DefaultRequestHeaders.Authorization =
            token == null ? null : new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<(bool ok, string? error)> RegisterAsync(string fullName, string email, string password, string role)
    {
        var res = await _http.PostAsJsonAsync("/api/auth/register", new { fullName, email, password, role });
        return res.IsSuccessStatusCode ? (true, null) : (false, await ErrorAsync(res));
    }

    public async Task<(bool ok, string? token, string? role, int userId, string? error)> LoginAsync(string email, string password)
    {
        var res = await _http.PostAsJsonAsync("/api/auth/login", new { email, password });
        if (!res.IsSuccessStatusCode) return (false, null, null, 0, await ErrorAsync(res));
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var r = doc.RootElement;
        return (true, r.GetProperty("token").GetString(), r.GetProperty("role").GetString(),
                r.GetProperty("userId").GetInt32(), null);
    }

    // ---- Events ----
    public async Task<List<EventVm>?> GetEventsAsync()
    {
        Auth();
        var res = await _http.GetAsync("/api/events");
        return res.IsSuccessStatusCode
            ? await res.Content.ReadFromJsonAsync<List<EventVm>>(Json) : null;
    }

    public async Task<EventVm?> GetEventAsync(int id)
    {
        Auth();
        var res = await _http.GetAsync($"/api/events/{id}");
        return res.IsSuccessStatusCode
            ? await res.Content.ReadFromJsonAsync<EventVm>(Json) : null;
    }

    public async Task<(bool ok, int? id, string? error)> CreateEventAsync(EventFormVm vm)
    {
        Auth();
        var res = await _http.PostAsJsonAsync("/api/events", vm);
        if (!res.IsSuccessStatusCode) return (false, null, await ErrorAsync(res));
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return (true, doc.RootElement.GetProperty("id").GetInt32(), null);
    }

    public async Task<(bool ok, string? error)> UpdateEventAsync(int id, EventFormVm vm)
    { Auth(); return (await _http.PutAsJsonAsync($"/api/events/{id}", vm)).IsSuccessStatusCode ? (true, null) : (false, await ErrorAsync(await _http.PutAsJsonAsync($"/api/events/{id}", vm))); }

    public async Task<bool> DeleteEventAsync(int id)
    { Auth(); return (await _http.DeleteAsync($"/api/events/{id}")).IsSuccessStatusCode; }

    public async Task<(bool ok, string? bannerUrl, string? error)> UploadBannerAsync(int eventId, IFormFile file)
    {
        Auth();
        using var content = new MultipartFormDataContent();
        using var stream = file.OpenReadStream();
        content.Add(new StreamContent(stream) { Headers = { ContentType = new MediaTypeHeaderValue(file.ContentType) } }, "file", file.FileName);
        var res = await _http.PostAsync($"/api/events/{eventId}/banner", content);
        if (!res.IsSuccessStatusCode) return (false, null, await ErrorAsync(res));
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return (true, doc.RootElement.GetProperty("bannerUrl").GetString(), null);
    }

    // ---- Categories / Enrolments / Results / Profile: same pattern ----
    public async Task<List<CategoryVm>?> GetCategoriesAsync(int eventId)
    { Auth(); var res = await _http.GetAsync($"/api/events/{eventId}/categories"); return res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<List<CategoryVm>>(Json) : null; }

    public async Task<(bool ok, string? error)> CreateCategoryAsync(int eventId, CategoryFormVm vm)
    { Auth(); var res = await _http.PostAsJsonAsync($"/api/events/{eventId}/categories", vm); return res.IsSuccessStatusCode ? (true, null) : (false, await ErrorAsync(res)); }

    public async Task<(bool ok, string? error)> EnrolAsync(int categoryId)
    { Auth(); var res = await _http.PostAsync($"/api/categories/{categoryId}/enrolments", null); return res.IsSuccessStatusCode ? (true, null) : (false, await ErrorAsync(res)); }

    public async Task<(bool ok, string? error)> CancelEnrolmentAsync(int id)
    { Auth(); var res = await _http.DeleteAsync($"/api/enrolments/{id}"); return res.IsSuccessStatusCode ? (true, null) : (false, await ErrorAsync(res)); }

    public async Task<List<EnrolmentVm>?> MyEnrolmentsAsync()
    { Auth(); var res = await _http.GetAsync("/api/users/me/enrolments"); return res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<List<EnrolmentVm>>(Json) : null; }

    public async Task<List<EnrolmentVm>?> CategoryEnrolmentsAsync(int categoryId)
    { Auth(); var res = await _http.GetAsync($"/api/categories/{categoryId}/enrolments"); return res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<List<EnrolmentVm>>(Json) : null; }

    public async Task<(bool ok, string? error)> AddResultAsync(int enrolmentId, ResultFormVm vm)
    { Auth(); var res = await _http.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", vm); return res.IsSuccessStatusCode ? (true, null) : (false, await ErrorAsync(res)); }

    public async Task<ProfileVm?> GetProfileAsync()
    { Auth(); var res = await _http.GetAsync("/api/users/me"); return res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<ProfileVm>(Json) : null; }

    public async Task<(bool ok, string? error)> UpdateProfileAsync(ProfileFormVm vm)
    { Auth(); var res = await _http.PutAsJsonAsync("/api/users/me", vm); return res.IsSuccessStatusCode ? (true, null) : (false, await ErrorAsync(res)); }

    public async Task<(bool ok, string? url, string? error)> UploadPictureAsync(IFormFile file)
    {
        Auth();
        using var content = new MultipartFormDataContent();
        using var stream = file.OpenReadStream();
        content.Add(new StreamContent(stream) { Headers = { ContentType = new MediaTypeHeaderValue(file.ContentType) } }, "file", file.FileName);
        var res = await _http.PostAsync("/api/users/me/picture", content);
        if (!res.IsSuccessStatusCode) return (false, null, await ErrorAsync(res));
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return (true, doc.RootElement.GetProperty("profilePicUrl").GetString(), null);
    }

    private static async Task<string?> ErrorAsync(HttpResponseMessage res)
    {
        var body = await res.Content.ReadAsStringAsync();
        try { using var doc = JsonDocument.Parse(body); if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("title", out var t)) return t.GetString(); } catch { }
        return $"{(int)res.StatusCode} {res.ReasonPhrase}";
    }
}