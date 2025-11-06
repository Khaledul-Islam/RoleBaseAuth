using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using RoleBaseAuth.Domain.Entities;

namespace RoleBaseAuth.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CustomerCode).IsRequired().HasMaxLength(20);
        builder.Property(c => c.CompanyName).IsRequired().HasMaxLength(200);
        builder.Property(c => c.ContactName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Phone).HasMaxLength(20);
        builder.Property(c => c.CreditLimit).HasPrecision(18, 2);

        builder.HasIndex(c => c.CustomerCode).IsUnique();
        builder.HasIndex(c => c.Email);
        builder.HasIndex(c => c.CompanyName);
    }
}