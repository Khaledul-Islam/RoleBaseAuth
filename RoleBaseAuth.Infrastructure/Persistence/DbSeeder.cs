using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RoleBaseAuth.Domain.Entities;
using RoleBaseAuth.Domain.Enum;
using RoleBaseAuth.Infrastructure.Identity;
using RoleBaseAuth.Infrastructure.Persistence.Contexts;

namespace RoleBaseAuth.Infrastructure.Persistence;

public class DbSeeder(
    ApplicationDbContext context,
    IAuthService authService,
    ILogger<DbSeeder> logger)
{
    public async Task SeedAsync()
    {
        try
        {
            await context.Database.MigrateAsync();

            if (!await context.Roles.AnyAsync())
            {
                await SeedRolesAsync();
            }

            if (!await context.Permissions.AnyAsync())
            {
                await SeedPermissionsAsync();
            }

            if (!await context.Users.AnyAsync())
            {
                await SeedUsersAsync();
            }

            if (!await context.Customers.AnyAsync())
            {
                await SeedCustomersAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
        }
    }

    private async Task SeedRolesAsync()
    {
        var roles = new[]
        {
            new Role { Id = Guid.NewGuid(), Name = Roles.SuperAdmin, Description = "Super Administrator", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new Role { Id = Guid.NewGuid(), Name = Roles.Admin, Description = "Administrator", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new Role { Id = Guid.NewGuid(), Name = Roles.Manager, Description = "Manager", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new Role { Id = Guid.NewGuid(), Name = Roles.User, Description = "Regular User", CreatedAt = DateTime.UtcNow, CreatedBy = "System" }
        };

        await context.Roles.AddRangeAsync(roles);
        await context.SaveChangesAsync();
        logger.LogInformation("Roles seeded successfully");
    }

    private async Task SeedPermissionsAsync()
    {
        var permissions = new[]
        {
            // Customer permissions
            new Permission { Id = Guid.NewGuid(), Name = Permissions.CustomersView, Description = "View customers", Resource = "Customers", Action = "View", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new Permission { Id = Guid.NewGuid(), Name = Permissions.CustomersCreate, Description = "Create customers", Resource = "Customers", Action = "Create", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new Permission { Id = Guid.NewGuid(), Name = Permissions.CustomersEdit, Description = "Edit customers", Resource = "Customers", Action = "Edit", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new Permission { Id = Guid.NewGuid(), Name = Permissions.CustomersDelete, Description = "Delete customers", Resource = "Customers", Action = "Delete", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new Permission { Id = Guid.NewGuid(), Name = Permissions.CustomersExport, Description = "Export customers", Resource = "Customers", Action = "Export", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            
            // User permissions
            new Permission { Id = Guid.NewGuid(), Name = Permissions.UsersView, Description = "View users", Resource = "Users", Action = "View", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new Permission { Id = Guid.NewGuid(), Name = Permissions.UsersCreate, Description = "Create users", Resource = "Users", Action = "Create", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new Permission { Id = Guid.NewGuid(), Name = Permissions.UsersEdit, Description = "Edit users", Resource = "Users", Action = "Edit", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new Permission { Id = Guid.NewGuid(), Name = Permissions.UsersDelete, Description = "Delete users", Resource = "Users", Action = "Delete", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            
            // Role permissions
            new Permission { Id = Guid.NewGuid(), Name = Permissions.RolesView, Description = "View roles", Resource = "Roles", Action = "View", CreatedAt = DateTime.UtcNow, CreatedBy = "System" },
            new Permission { Id = Guid.NewGuid(), Name = Permissions.RolesManage, Description = "Manage roles", Resource = "Roles", Action = "Manage", CreatedAt = DateTime.UtcNow, CreatedBy = "System" }
        };

        await context.Permissions.AddRangeAsync(permissions);
        await context.SaveChangesAsync();

        // Assign all permissions to SuperAdmin
        var superAdminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == Roles.SuperAdmin);
        if (superAdminRole != null)
        {
            var rolePermissions = permissions.Select(p => new RolePermission
            {
                RoleId = superAdminRole.Id,
                PermissionId = p.Id,
                GrantedAt = DateTime.UtcNow,
                GrantedBy = "System"
            }).ToList();

            await context.RolePermissions.AddRangeAsync(rolePermissions);
        }

        // Assign customer permissions to Manager
        var managerRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == Roles.Manager);
        if (managerRole != null)
        {
            var managerPermissions = permissions
                .Where(p => p.Resource == "Customers")
                .Select(p => new RolePermission
                {
                    RoleId = managerRole.Id,
                    PermissionId = p.Id,
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = "System"
                }).ToList();

            await context.RolePermissions.AddRangeAsync(managerPermissions);
        }

        // Assign view permission to User
        var userRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == Roles.User);
        if (userRole != null)
        {
            var userPermissions = permissions
                .Where(p => p.Action == "View")
                .Select(p => new RolePermission
                {
                    RoleId = userRole.Id,
                    PermissionId = p.Id,
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = "System"
                }).ToList();

            await context.RolePermissions.AddRangeAsync(userPermissions);
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Permissions seeded successfully");
    }

    private async Task SeedUsersAsync()
    {
        var superAdminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == Roles.SuperAdmin);

        var superAdmin = new User
        {
            Id = Guid.NewGuid(),
            Username = "superadmin",
            Email = "superadmin@example.com",
            PasswordHash = authService.HashPassword("SuperAdmin@123"),
            FirstName = "Super",
            LastName = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        await context.Users.AddAsync(superAdmin);
        await context.SaveChangesAsync();

        if (superAdminRole != null)
        {
            await context.UserRoles.AddAsync(new UserRole
            {
                UserId = superAdmin.Id,
                RoleId = superAdminRole.Id,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = "System"
            });
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Default users seeded successfully");
    }

    private async Task SeedCustomersAsync()
    {
        var customers = new[]
        {
            new Customer
            {
                Id = Guid.NewGuid(),
                CustomerCode = "CUST000001",
                CompanyName = "Acme Corporation",
                ContactName = "John Doe",
                Email = "john.doe@acme.com",
                Phone = "+1-555-0001",
                Address = "123 Main St",
                City = "New York",
                Country = "USA",
                PostalCode = "10001",
                CreditLimit = 50000,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            },
            new Customer
            {
                Id = Guid.NewGuid(),
                CustomerCode = "CUST000002",
                CompanyName = "Global Tech Solutions",
                ContactName = "Jane Smith",
                Email = "jane.smith@globaltech.com",
                Phone = "+1-555-0002",
                Address = "456 Oak Ave",
                City = "San Francisco",
                Country = "USA",
                PostalCode = "94102",
                CreditLimit = 75000,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            }
        };

        await context.Customers.AddRangeAsync(customers);
        await context.SaveChangesAsync();
        logger.LogInformation("Sample customers seeded successfully");
    }
}