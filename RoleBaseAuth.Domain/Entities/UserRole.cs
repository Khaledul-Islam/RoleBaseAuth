namespace RoleBaseAuth.Domain.Entities;

public class UserRole
{
    public Guid UserId { get; set; }
    public User User { get; set; } = new();

    public Guid RoleId { get; set; }
    public Role Role { get; set; } = new();

    public DateTime AssignedAt { get; set; }
    public string AssignedBy { get; set; } = string.Empty;
}