using RoleBaseAuth.Application.Common.Models;
using System.Net;
using FluentValidation;

namespace RoleBaseAuth.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        logger.LogError(exception, "An unhandled exception occurred");

        var response = context.Response;
        response.ContentType = "application/json";

        var apiResponse = new ApiResponse<object>
        {
            Success = false,
            Timestamp = DateTime.UtcNow
        };

        switch (exception)
        {
            case ValidationException validationException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                apiResponse.Message = "Validation failed";
                apiResponse.Errors = validationException.Errors
                    .Select(e => e.ErrorMessage)
                    .ToList();
                break;

            case UnauthorizedAccessException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                apiResponse.Message = "Unauthorized access";
                break;

            case KeyNotFoundException:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                apiResponse.Message = "Resource not found";
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                apiResponse.Message = "An internal server error occurred";
                apiResponse.Errors = new List<string> { exception.Message };
                break;
        }

        await response.WriteAsJsonAsync(apiResponse);
    }
}