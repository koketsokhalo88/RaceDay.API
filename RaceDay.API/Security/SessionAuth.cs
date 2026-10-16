using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Security;

public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
}

/// <summary>Blocks the request unless a user is logged in (401) and, optionally, has the given role (403).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class SessionAuthAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? _role;
    public SessionAuthAttribute(string? role = null) => _role = role;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        if (session.GetInt32(SessionKeys.UserId) is null)
        {
            context.Result = new ObjectResult(new { message = "You must be logged in." }) { StatusCode = 401 };
            return;
        }
        if (_role != null && session.GetString(SessionKeys.Role) != _role)
        {
            context.Result = new ObjectResult(new { message = $"This action requires the {_role} role." }) { StatusCode = 403 };
        }
    }
}

public abstract class ApiControllerBase : ControllerBase
{
    protected int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId) ?? 0;
    protected string? CurrentRole => HttpContext.Session.GetString(SessionKeys.Role);

    protected IActionResult Forbidden(string message = "You do not have permission to do this.")
        => StatusCode(StatusCodes.Status403Forbidden, new { message });
}