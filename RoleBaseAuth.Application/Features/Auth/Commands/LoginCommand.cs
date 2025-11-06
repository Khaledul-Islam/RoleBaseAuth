using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RoleBaseAuth.Application.Common.Models;
using RoleBaseAuth.Application.DTOs.Auth;
using RoleBaseAuth.Domain.Entities;
using RoleBaseAuth.Domain.Interfaces;
using RoleBaseAuth.Infrastructure.Identity;

namespace RoleBaseAuth.Application.Features.Auth.Commands;

// Login Command
public class LoginCommand : IRequest<ApiResponse<LoginResponse>>
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}

public class LoginCommandHandler(
    IUnitOfWork unitOfWork,
    IAuthService authService,
    IMapper mapper,
    ILogger<LoginCommandHandler> logger)
    : IRequestHandler<LoginCommand, ApiResponse<LoginResponse>>
{
    public async Task<ApiResponse<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var users = await unitOfWork.Users.FindAsync(u => u.Username == request.Username, cancellationToken);
            var user = users.FirstOrDefault();

            if (user == null)
            {
                logger.LogWarning("Login attempt with non-existent username: {Username}", request.Username);
                return ApiResponse<LoginResponse>.ErrorResponse("Invalid username or password");
            }

            // Check lockout
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                logger.LogWarning("Login attempt for locked account: {Username}", request.Username);
                return ApiResponse<LoginResponse>.ErrorResponse("Account is locked. Please try again later.");
            }

            // Verify password
            if (!authService.VerifyPassword(request.Password, user.PasswordHash))
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
                    logger.LogWarning("Account locked due to multiple failed attempts: {Username}", request.Username);
                }
                await unitOfWork.SaveChangesAsync(cancellationToken);

                return ApiResponse<LoginResponse>.ErrorResponse("Invalid username or password");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                return ApiResponse<LoginResponse>.ErrorResponse("Account is inactive");
            }

            // Generate tokens
            var accessToken = authService.GenerateAccessToken(user);
            var refreshToken = authService.GenerateRefreshToken();

            // Save refresh token
            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                JwtId = accessToken.JwtId,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };
            await unitOfWork.RefreshTokens.AddAsync(refreshTokenEntity, cancellationToken);

            // Update user login info
            user.LastLoginAt = DateTime.UtcNow;
            user.LastLoginIp = request.IpAddress;
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await unitOfWork.Users.UpdateAsync(user, cancellationToken);

            // Log audit
            await unitOfWork.AuditLogs.AddAsync(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Action = "Login",
                EntityName = "User",
                EntityId = user.Id.ToString(),
                IpAddress = request.IpAddress,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var userDto = mapper.Map<UserDto>(user);
            var response = new LoginResponse
            {
                AccessToken = accessToken.Token,
                RefreshToken = refreshToken,
                ExpiresAt = accessToken.ExpiresAt,
                User = userDto
            };

            return ApiResponse<LoginResponse>.SuccessResponse(response, "Login successful");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during login for username: {Username}", request.Username);
            return ApiResponse<LoginResponse>.ErrorResponse("An error occurred during login");
        }
    }
}