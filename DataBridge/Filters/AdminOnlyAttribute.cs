using DataBridge.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DataBridge.Filters
{
    public class AdminOnlyAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var role = context.HttpContext.Session.GetString("Role");
            if (role != UserRole.Admin.ToString())
            {
                context.Result = new RedirectToActionResult("Index", "AccessDenied", null);
            }
        }
    }
}