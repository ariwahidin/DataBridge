namespace DataBridge.Middleware
{
    public class AuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthMiddleware> _logger;
        private static readonly string[] _publicPaths =
        [
            "/Auth/Login",
            "/Auth/Logout",
            "/css/",
            "/js/",
            "/lib/",
            "/favicon.ico"
        ];

        public AuthMiddleware(RequestDelegate next, ILogger<AuthMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "";
            var userId = context.Session.GetString("UserId");

            _logger.LogInformation(">>> PATH: {Path} | UserId in session: {UserId}", path, userId ?? "NULL");

            bool isPublic = _publicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

            if (!isPublic && userId == null)
            {
                _logger.LogWarning(">>> REDIRECTING to login, session empty");
                context.Response.Redirect("/Auth/Login");
                return;
            }

            await _next(context);
        }
    }
}