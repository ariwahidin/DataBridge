using DataBridge.Services;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DataBridge.Filters
{
    /// <summary>
    /// Pasang di action POST/destructive. GET biasanya tidak perlu di-log.
    /// Contoh: [LogActivity("MirrorJobs", "Create")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class LogActivityAttribute : Attribute, IAsyncActionFilter
    {
        private readonly string _module;
        private readonly string _action;
        private readonly string? _description;

        public LogActivityAttribute(string module, string action, string? description = null)
        {
            _module = module;
            _action = action;
            _description = description;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executed = await next();

            // Hanya log kalau bukan redirect ke error / exception
            var isSuccess = executed.Exception == null || executed.ExceptionHandled;
            var errMsg = executed.Exception?.Message;

            var svc = context.HttpContext.RequestServices.GetService<ActivityLogService>();
            svc?.Log(_module, _action, _description, isSuccess, errMsg);
        }
    }
}