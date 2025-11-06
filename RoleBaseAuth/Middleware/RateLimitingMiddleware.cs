using System.Security.Claims;

namespace RoleBaseAuth.Middleware;

public class RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
{
    private static readonly Dictionary<string, (int count, DateTime resetTime)> _requestCounts = new();
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString();
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var key = !string.IsNullOrEmpty(userId) ? $"user:{userId}" : $"ip:{clientIp}";

        await _semaphore.WaitAsync();
        try
        {
            if (!_requestCounts.TryGetValue(key, out var requestInfo))
            {
                requestInfo = (1, DateTime.UtcNow.AddMinutes(1));
                _requestCounts[key] = requestInfo;
            }
            else
            {
                if (DateTime.UtcNow > requestInfo.resetTime)
                {
                    requestInfo = (1, DateTime.UtcNow.AddMinutes(1));
                    _requestCounts[key] = requestInfo;
                }
                else
                {
                    if (requestInfo.count >= 100) // 100 requests per minute
                    {
                        logger.LogWarning("Rate limit exceeded for {Key}", key);
                        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            error = "Rate limit exceeded. Please try again later."
                        });
                        return;
                    }

                    _requestCounts[key] = (requestInfo.count + 1, requestInfo.resetTime);
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }

        await next(context);
    }
}