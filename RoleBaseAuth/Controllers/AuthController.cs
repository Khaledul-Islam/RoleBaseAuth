using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoleBaseAuth.Application.Common.Models;
using RoleBaseAuth.Application.DTOs.Auth;
using RoleBaseAuth.Application.Features.Auth.Commands;

namespace RoleBaseAuth.Controllers
{
    [AllowAnonymous]
    public class AuthController : BaseApiController
    {
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var command = new LoginCommand
            {
                Username = request.Username,
                Password = request.Password,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            var result = await Mediator.Send(command);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var command = new RegisterCommand
            {
                Username = request.Username,
                Email = request.Email,
                Password = request.Password,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            var result = await Mediator.Send(command);

            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(Register), result);
        }

        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var command = new RefreshTokenCommand
            {
                RefreshToken = request.RefreshToken,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            var result = await Mediator.Send(command);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // Implement logout logic (revoke refresh tokens)
            return Ok(new { message = "Logged out successfully" });
        }
    }
}
