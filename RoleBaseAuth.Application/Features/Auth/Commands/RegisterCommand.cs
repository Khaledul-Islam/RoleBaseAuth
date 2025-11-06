using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RoleBaseAuth.Application.Common.Models;
using RoleBaseAuth.Application.DTOs.Auth;
using RoleBaseAuth.Domain.Entities;
using RoleBaseAuth.Domain.Enum;
using RoleBaseAuth.Domain.Interfaces;
using RoleBaseAuth.Infrastructure.Identity;

namespace RoleBaseAuth.Application.Features.Auth.Commands;

public class RegisterCommand : IRequest<ApiResponse<UserDto>>
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class RegisterCommandHandler(
    IUnitOfWork unitOfWork,
    IAuthService authService,
    IMapper mapper,
    ILogger<RegisterCommandHandler> logger)
    : IRequestHandler<RegisterCommand, ApiResponse<UserDto>>
{
    public async Task<ApiResponse<UserDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if username exists
            var existingUsers = await unitOfWork.Users.FindAsync(
                u => u.Username == request.Username || u.Email == request.Email,
                cancellationToken);

            if (existingUsers.Any())
            {
                return ApiResponse<UserDto>.ErrorResponse("Username or email already exists");
            }

            // Hash password
            var passwordHash = authService.HashPassword(request.Password);

            // Create user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await unitOfWork.Users.AddAsync(user, cancellationToken);

            // Assign default role
            var roles = await unitOfWork.Roles.FindAsync(r => r.Name == Roles.User, cancellationToken);
            var userRole = roles.FirstOrDefault();
            if (userRole != null)
            {
                var userRoleEntity = new UserRole
                {
                    UserId = user.Id,
                    RoleId = userRole.Id,
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = "System"
                };
                // This would need a UserRole repository or direct context access
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var userDto = mapper.Map<UserDto>(user);
            logger.LogInformation("New user registered: {Username}", request.Username);

            return ApiResponse<UserDto>.SuccessResponse(userDto, "Registration successful");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during registration for username: {Username}", request.Username);
            return ApiResponse<UserDto>.ErrorResponse("An error occurred during registration");
        }
    }
}