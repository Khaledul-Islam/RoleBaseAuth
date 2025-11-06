using RoleBaseAuth.Domain.Common;
using System.Collections;

namespace RoleBaseAuth.Domain.Entities;

public class Permission : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;          

    public ICollection RolePermissions { get; set; } = new List<RolePermission>();
}