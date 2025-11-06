namespace RoleBaseAuth.Application.DTOs.Auth;

public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;    
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;    
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}