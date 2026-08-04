namespace Quản_lý_quán_cafe.Middleware
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public LoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                var logger = context.RequestServices.GetService<ILogger<LoggingMiddleware>>();
                logger?.LogInformation("Request: {Method} {Path}", context.Request.Method, context.Request.Path);

                await _next(context);

                logger?.LogInformation("Response: {StatusCode}", context.Response.StatusCode);
            }
            catch (Exception)
            {
                await _next(context);
            }
        }
    }
}
