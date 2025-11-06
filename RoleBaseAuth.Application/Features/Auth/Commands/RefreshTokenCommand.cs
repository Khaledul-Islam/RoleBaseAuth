using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RoleBaseAuth.Application.Common.Models;
using RoleBaseAuth.Application.DTOs.Auth;
using RoleBaseAuth.Domain.Entities;
using RoleBaseAuth.Domain.Interfaces;
using RoleBaseAuth.Infrastructure.Identity;

namespace RoleBaseAuth.Application.Features.Auth.Commands;

public class RefreshTokenCommand : IRequest<ApiResponse<LoginResponse>>
{
    public string RefreshToken { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}

public class RefreshTokenCommandHandler(
    IUnitOfWork unitOfWork,
    IAuthService authService,
    IMapper mapper,
    ILogger<RefreshTokenCommandHandler> logger)
    : IRequestHandler<RefreshTokenCommand, ApiResponse<LoginResponse>>
{
    public async Task<ApiResponse<LoginResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tokens = await unitOfWork.RefreshTokens.FindAsync(
                rt => rt.Token == request.RefreshToken && !rt.IsRevoked,
                cancellationToken);
            var refreshToken = tokens.FirstOrDefault();

            if (refreshToken == null || refreshToken.ExpiresAt < DateTime.UtcNow)
            {
                return ApiResponse<LoginResponse>.ErrorResponse("Invalid or expired refresh token");
            }

            var user = await unitOfWork.Users.GetByIdAsync(refreshToken.UserId, cancellationToken);
            if (user == null || !user.IsActive)
            {
                return ApiResponse<LoginResponse>.ErrorResponse("User not found or inactive");
            }

            // Revoke old token
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = request.IpAddress;
            await unitOfWork.RefreshTokens.UpdateAsync(refreshToken, cancellationToken);

            // Generate new tokens
            var accessToken = authService.GenerateAccessToken(user);
            var newRefreshToken = authService.GenerateRefreshToken();

            refreshToken.ReplacedByToken = newRefreshToken;

            // Save new refresh token
            var newRefreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = newRefreshToken,
                JwtId = accessToken.JwtId,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };
            await unitOfWork.RefreshTokens.AddAsync(newRefreshTokenEntity, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var userDto = mapper.Map<UserDto>(user);
            var response = new LoginResponse
            {
                AccessToken = accessToken.Token,
                RefreshToken = newRefreshToken,
                ExpiresAt = accessToken.ExpiresAt,
                User = userDto
            };

            return ApiResponse<LoginResponse>.SuccessResponse(response, "Token refreshed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during token refresh");
            return ApiResponse<LoginResponse>.ErrorResponse("An error occurred during token refresh");
        }
    }
}