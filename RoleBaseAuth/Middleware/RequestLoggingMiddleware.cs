namespace RoleBaseAuth.Middleware;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = Guid.NewGuid().ToString();
        context.Items["RequestId"] = requestId;

        logger.LogInformation(
            "HTTP {Method} {Path} started - RequestId: {RequestId}",
            context.Request.Method,
            context.Request.Path,
            requestId);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await next(context);
        }
        finally
        {
            sw.Stop();
            logger.LogInformation(
                "HTTP {Method} {Path} completed with {StatusCode} in {ElapsedMs}ms - RequestId: {RequestId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds,
                requestId);
        }
    }
}