using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Security;
using RaceDayAPI.Services;


namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users")]
[SessionAuth]
public class UsersController : ApiControllerBase
{
    private readonly AppDbContext _db;
    private readonly BlobService _blob;
    public UsersController(AppDbContext db, BlobService blob)
    {
        _db = db;
        _blob = blob;
    }

    /// <summary>Gets the profile of the logged-in user. Any role.</summary>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserID == CurrentUserId);
        if (user is null) return Unauthorized(new { message = "Account no longer exists." });
        return Ok(Map.ToUser(user, user.Role.RoleName));
    }

    /// <summary>Updates the logged-in user's name and email. Any role. Returns 409 if the email is taken.</summary>
    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateMe(UpdateProfileDto dto)
    {
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserID == CurrentUserId);
        if (user is null) return Unauthorized(new { message = "Account no longer exists." });

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email && u.UserID != user.UserID))
            return Conflict(new { message = "That email is already registered." });

        user.FullName = dto.FullName.Trim();
        user.Email = email;
        await _db.SaveChangesAsync();
        return Ok(Map.ToUser(user, user.Role.RoleName));
    }

    /// <summary>Lists the logged-in participant's enrolments with event details and results. Participant only.</summary>
    [HttpGet("me/enrolments"), SessionAuth(Roles.Participant)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MyEnrolments()
    {
        var list = await _db.Enrolments.AsNoTracking()
            .Where(e => e.ParticipantID == CurrentUserId)
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Include(e => e.Result)
            .ToListAsync();

        return Ok(list.Select(e => new
        {
            enrolmentId = e.EnrolmentID,
            status = e.Status,
            enrolmentDate = e.EnrolmentDate,
            category = Map.ToCategory(e.Category),
            eventName = e.Category.Event.EventName,
            eventDate = e.Category.Event.EventDate,
            result = e.Result == null ? null : Map.ToResult(e.Result)
        }));
    }

    public Task<IActionResult> UploadProfilePic(IFormFile file)
    {
        return UploadProfilePic(file, CurrentUserId);
    }

    /// <summary>Uploads a profile picture for the logged-in user.</summary>
    [HttpPost("me/profile-pic")]
    public async Task<IActionResult> UploadProfilePic(IFormFile file, int currentUserId)
    {
        var user = await _db.Users.FindAsync(currentUserId);
        if (user == null) return NotFound();

        if (file == null || file.Length == 0) return BadRequest("No file uploaded");

        await _blob.DeleteAsync(user.ProfilePicUrl);

        user.ProfilePicUrl = await _blob.UploadAsync(file);

        await _db.SaveChangesAsync();
        return Ok(new { profilePicUrl = user.ProfilePicUrl });
    }  
}
