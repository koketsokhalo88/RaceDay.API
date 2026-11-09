using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDayMVC.Filters;

public class RequireRoleAttribute : ActionFilterAttribute
{
    private readonly string? _role;
    public RequireRoleAttribute(string? role = null) => _role = role;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var s = context.HttpContext.Session;
        if (string.IsNullOrEmpty(s.GetString("token")))
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }
        if (_role != null && s.GetString("role") != _role)
            context.Result = new RedirectToActionResult("Index", "Home", null);
    }
}
