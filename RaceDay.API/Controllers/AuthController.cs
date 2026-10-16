using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly AppDbContext _db;
    public AuthController(AppDbContext db) => _db = db;

    /// <summary>Registers a new account as an Organiser or a Participant. Public. The password is stored as a BCrypt hash.</summary>
    /// <remarks>Expects: fullName, email, password (min 8 chars), role ("Organiser" or "Participant"). Returns 201, 400 (validation / unknown role) or 409 (email already registered).</remarks>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var roles = await _db.Roles.ToListAsync();
        var role = roles.FirstOrDefault(r => r.RoleName.Equals(dto.Role, StringComparison.OrdinalIgnoreCase));
        if (role is null) return BadRequest(new { message = "Role must be 'Organiser' or 'Participant'." });

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "That email is already registered." });

        var user = new AppUser
        {
            RoleID = role.RoleID,
            FullName = dto.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Created("/api/users/me", Map.ToUser(user, role.RoleName));
    }

    /// <summary>Logs a user in and starts a session that remembers their identity and role. Public.</summary>
    /// <remarks>Expects: email, password. Returns 200 with userId, fullName and role, or 401 for invalid credentials. Swagger keeps the session cookie automatically.</remarks>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserID);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role.RoleName);
        return Ok(new { userId = user.UserID, fullName = user.FullName, role = user.Role.RoleName });
    }

    /// <summary>Ends the current session. Any logged-in user.</summary>
    [HttpPost("logout"), SessionAuth]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return NoContent();
    }
}