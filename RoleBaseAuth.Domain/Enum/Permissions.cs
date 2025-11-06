namespace RoleBaseAuth.Domain.Enum;

public static class Permissions
{
    // Customer Permissions
    public const string CustomersView = "customers.view";
    public const string CustomersCreate = "customers.create";
    public const string CustomersEdit = "customers.edit";
    public const string CustomersDelete = "customers.delete";
    public const string CustomersExport = "customers.export";

    // User Management
    public const string UsersView = "users.view";
    public const string UsersCreate = "users.create";
    public const string UsersEdit = "users.edit";
    public const string UsersDelete = "users.delete";

    // Role Management
    public const string RolesView = "roles.view";
    public const string RolesManage = "roles.manage";
}