namespace RoleBaseAuth.Domain.Entities;

public class RolePermission
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = new();

    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = new();

    public DateTime GrantedAt { get; set; }
    public string GrantedBy { get; set; } = string.Empty;
}