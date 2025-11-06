using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace RoleBaseAuth.Infrastructure.Identity;

public interface ICurrentUserService
{
    string UserId { get; }
    string Username { get; }
    string Email { get; }
    string IpAddress { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    bool HasPermission(string permission);
}

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string UserId => httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public string Username =>  httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;

    public string Email =>  httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;
    public string IpAddress => httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role)
    {
        return httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
    }

    public bool HasPermission(string permission)
    {
        return httpContextAccessor.HttpContext?.User?.HasClaim("Permission", permission) ?? false;
    }
}