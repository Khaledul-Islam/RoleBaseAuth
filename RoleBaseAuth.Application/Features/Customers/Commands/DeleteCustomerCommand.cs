using MediatR;
using Microsoft.Extensions.Logging;
using RoleBaseAuth.Application.Common.Models;
using RoleBaseAuth.Domain.Entities;
using RoleBaseAuth.Domain.Interfaces;
using RoleBaseAuth.Infrastructure.Identity;

namespace RoleBaseAuth.Application.Features.Customers.Commands;

public class DeleteCustomerCommand : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; }
}

public class DeleteCustomerCommandHandler(
    IUnitOfWork unitOfWork,
    ILogger<DeleteCustomerCommandHandler> logger,
    ICurrentUserService currentUser)
    : IRequestHandler<DeleteCustomerCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await unitOfWork.Customers.GetByIdAsync(request.Id, cancellationToken);
            if (customer == null || customer.IsDeleted)
            {
                return ApiResponse<bool>.ErrorResponse("Customer not found");
            }

            // Soft delete
            customer.IsDeleted = true;
            customer.DeletedAt = DateTime.UtcNow;
            customer.DeletedBy = currentUser.UserId;

            await unitOfWork.Customers.UpdateAsync(customer, cancellationToken);

            // Audit log
            await unitOfWork.AuditLogs.AddAsync(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = Guid.Parse(currentUser.UserId),
                Action = "Delete",
                EntityName = "Customer",
                EntityId = customer.Id.ToString(),
                IpAddress = currentUser.IpAddress,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Customer deleted: {CustomerCode}", customer.CustomerCode);

            return ApiResponse<bool>.SuccessResponse(true, "Customer deleted successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting customer {CustomerId}", request.Id);
            return ApiResponse<bool>.ErrorResponse("An error occurred while deleting the customer");
        }
    }
}