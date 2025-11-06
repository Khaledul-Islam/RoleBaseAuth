using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RoleBaseAuth.Application.Common.Models;
using RoleBaseAuth.Application.DTOs.Customers;
using RoleBaseAuth.Domain.Interfaces;

namespace RoleBaseAuth.Application.Features.Customers.Queries;

public class GetAllCustomersQuery : IRequest<ApiResponse<PagedResult<CustomerDto>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SearchTerm { get; set; } = string.Empty;
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
    public bool? IsActive { get; set; }
}

public class GetAllCustomersQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetAllCustomersQuery, ApiResponse<PagedResult<CustomerDto>>>
{
    public async Task<ApiResponse<PagedResult<CustomerDto>>> Handle(GetAllCustomersQuery request, CancellationToken cancellationToken)
    {
        var customers = await unitOfWork.Customers.FindAsync(
            c => !c.IsDeleted &&
                 (!request.IsActive.HasValue || c.IsActive == request.IsActive) &&
                 (string.IsNullOrEmpty(request.SearchTerm) ||
                  c.CompanyName.Contains(request.SearchTerm) ||
                  c.ContactName.Contains(request.SearchTerm) ||
                  c.Email.Contains(request.SearchTerm)),
            cancellationToken);

        var totalCount = customers.Count();

        // Apply sorting
        var sortedCustomers = request.SortDescending
            ? customers.OrderByDescending(c => EF.Property<object>(c, request.SortBy))
            : customers.OrderBy(c => EF.Property<object>(c, request.SortBy));

        // Apply pagination
        var paginatedCustomers = sortedCustomers
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var customerDtos = mapper.Map<List<CustomerDto>>(paginatedCustomers);

        var result = new PagedResult<CustomerDto>
        {
            Items = customerDtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        return ApiResponse<PagedResult<CustomerDto>>.SuccessResponse(result);
    }
}