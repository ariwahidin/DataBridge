using DataBridge.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DataBridge.Filters
{
    public class RequireRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly UserRole[] _roles;

        public RequireRoleAttribute(params UserRole[] roles)
        {
            _roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var roleStr = context.HttpContext.Session.GetString("Role");
            if (!Enum.TryParse<UserRole>(roleStr, out var role) || !_roles.Contains(role))
            {
                context.Result = new RedirectToActionResult("Index", "AccessDenied", null);
            }
        }
    }
}