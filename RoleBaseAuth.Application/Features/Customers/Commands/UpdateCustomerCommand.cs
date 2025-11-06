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

public class UpdateCustomerCommand : IRequest<ApiResponse<CustomerDto>>
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public bool IsActive { get; set; }
}

public class UpdateCustomerCommandHandler(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ILogger<UpdateCustomerCommandHandler> logger,
    ICurrentUserService currentUser)
    : IRequestHandler<UpdateCustomerCommand, ApiResponse<CustomerDto>>
{
    public async Task<ApiResponse<CustomerDto>> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await unitOfWork.Customers.GetByIdAsync(request.Id, cancellationToken);
            if (customer == null || customer.IsDeleted)
            {
                return ApiResponse<CustomerDto>.ErrorResponse("Customer not found");
            }

            var oldValues = JsonSerializer.Serialize(customer);

            // Update properties
            customer.CompanyName = request.CompanyName;
            customer.ContactName = request.ContactName;
            customer.Email = request.Email;
            customer.Phone = request.Phone;
            customer.Address = request.Address;
            customer.City = request.City;
            customer.Country = request.Country;
            customer.PostalCode = request.PostalCode;
            customer.CreditLimit = request.CreditLimit;
            customer.IsActive = request.IsActive;
            customer.UpdatedAt = DateTime.UtcNow;
            customer.UpdatedBy = currentUser.UserId;

            await unitOfWork.Customers.UpdateAsync(customer, cancellationToken);

            // Audit log
            await unitOfWork.AuditLogs.AddAsync(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = Guid.Parse(currentUser.UserId),
                Action = "Update",
                EntityName = "Customer",
                EntityId = customer.Id.ToString(),
                OldValues = oldValues,
                NewValues = JsonSerializer.Serialize(customer),
                IpAddress = currentUser.IpAddress,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var customerDto = mapper.Map<CustomerDto>(customer);
            logger.LogInformation("Customer updated: {CustomerCode}", customer.CustomerCode);

            return ApiResponse<CustomerDto>.SuccessResponse(customerDto, "Customer updated successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating customer {CustomerId}", request.Id);
            return ApiResponse<CustomerDto>.ErrorResponse("An error occurred while updating the customer");
        }
    }
}