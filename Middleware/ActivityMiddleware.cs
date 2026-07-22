namespace CafeManagement.Middleware
{
    public class ActivityMiddleware
    {
        private readonly RequestDelegate _next;

        public ActivityMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Track user activity
            if (context.User.Identity?.IsAuthenticated == true)
            {
                context.Items["UserId"] = context.User.FindFirst("sub")?.Value ?? context.User.Identity.Name;
                context.Items["ActivityTime"] = DateTime.UtcNow;
            }

            await _next(context);
        }
    }
}
