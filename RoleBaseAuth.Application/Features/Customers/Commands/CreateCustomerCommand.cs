using System.Text.Json;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RoleBaseAuth.Application.Common.Models;
using RoleBaseAuth.Application.DTOs.Customers;
using RoleBaseAuth.Domain.Entities;
using RoleBaseAuth.Domain.Interfaces;
using RoleBaseAuth.Infrastructure.Identity;

namespace RoleBaseAuth.Application.Features.Customers.Commands;

public class CreateCustomerCommand : IRequest<ApiResponse<CustomerDto>>
{
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
}

public class CreateCustomerCommandHandler(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ILogger<CreateCustomerCommandHandler> logger,
    ICurrentUserService currentUser)
    : IRequestHandler<CreateCustomerCommand, ApiResponse<CustomerDto>>
{
    public async Task<ApiResponse<CustomerDto>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check for duplicate email
            var existingCustomers = await unitOfWork.Customers.FindAsync(
                c => c.Email == request.Email && !c.IsDeleted,
                cancellationToken);

            if (existingCustomers.Any())
            {
                return ApiResponse<CustomerDto>.ErrorResponse("A customer with this email already exists");
            }

            // Generate customer code
            var customerCount = await unitOfWork.Customers.CountAsync(cancellationToken: cancellationToken);
            var customerCode = $"CUST{(customerCount + 1):D6}";

            var customer = mapper.Map<Customer>(request);
            customer.Id = Guid.NewGuid();
            customer.CustomerCode = customerCode;
            customer.IsActive = true;
            customer.CreatedAt = DateTime.UtcNow;
            customer.CreatedBy = currentUser.UserId;

            await unitOfWork.Customers.AddAsync(customer, cancellationToken);

            // Audit log
            await unitOfWork.AuditLogs.AddAsync(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = Guid.Parse(currentUser.UserId),
                Action = "Create",
                EntityName = "Customer",
                EntityId = customer.Id.ToString(),
                NewValues = JsonSerializer.Serialize(customer),
                IpAddress = currentUser.IpAddress,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var customerDto = mapper.Map<CustomerDto>(customer);
            logger.LogInformation("Customer created: {CustomerCode}", customerCode);

            return ApiResponse<CustomerDto>.SuccessResponse(customerDto, "Customer created successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating customer");
            return ApiResponse<CustomerDto>.ErrorResponse("An error occurred while creating the customer");
        }
    }
}