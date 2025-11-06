using AutoMapper;
using MediatR;
using RoleBaseAuth.Application.Common.Models;
using RoleBaseAuth.Application.DTOs.Customers;
using RoleBaseAuth.Domain.Interfaces;
using RoleBaseAuth.Infrastructure.Caching;

namespace RoleBaseAuth.Application.Features.Customers.Queries;

public class GetCustomerByIdQuery : IRequest<ApiResponse<CustomerDto>>
{
    public Guid Id { get; set; }
}

public class GetCustomerByIdQueryHandler(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ICacheService cache)
    : IRequestHandler<GetCustomerByIdQuery, ApiResponse<CustomerDto>>
{
    public async Task<ApiResponse<CustomerDto>> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"customer:{request.Id}";
        var cachedCustomer = await cache.GetAsync<CustomerDto>(cacheKey);

        if (cachedCustomer != null)
            return ApiResponse<CustomerDto>.SuccessResponse(cachedCustomer);

        var customer = await unitOfWork.Customers.GetByIdAsync(request.Id, cancellationToken);

        if (customer == null || customer.IsDeleted)
            return ApiResponse<CustomerDto>.ErrorResponse("Customer not found");

        var customerDto = mapper.Map<CustomerDto>(customer);
        await cache.SetAsync(cacheKey, customerDto, TimeSpan.FromMinutes(10));

        return ApiResponse<CustomerDto>.SuccessResponse(customerDto);
    }
}