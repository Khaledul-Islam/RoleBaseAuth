using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoleBaseAuth.Application.Common.Models;
using RoleBaseAuth.Application.DTOs.Customers;
using RoleBaseAuth.Application.Features.Customers.Commands;
using RoleBaseAuth.Application.Features.Customers.Queries;

namespace RoleBaseAuth.Controllers
{
    [Authorize]
    public class CustomersController : BaseApiController
    {
        [HttpGet]
        [Authorize(Policy = "RequireCustomersViewPermission")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<CustomerDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] GetAllCustomersQuery query)
        {
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "RequireCustomersViewPermission")]
        [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetCustomerByIdQuery { Id = id };
            var result = await Mediator.Send(query);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "RequireCustomersCreatePermission")]
        [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request)
        {
            var command = new CreateCustomerCommand
            {
                CompanyName = request.CompanyName,
                ContactName = request.ContactName,
                Email = request.Email,
                Phone = request.Phone,
                Address = request.Address,
                City = request.City,
                Country = request.Country,
                PostalCode = request.PostalCode,
                CreditLimit = request.CreditLimit
            };

            var result = await Mediator.Send(command);

            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "RequireCustomersEditPermission")]
        [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest request)
        {
            if (id != request.Id)
                return BadRequest("ID mismatch");

            var command = new UpdateCustomerCommand
            {
                Id = request.Id,
                CompanyName = request.CompanyName,
                ContactName = request.ContactName,
                Email = request.Email,
                Phone = request.Phone,
                Address = request.Address,
                City = request.City,
                Country = request.Country,
                PostalCode = request.PostalCode,
                CreditLimit = request.CreditLimit,
                IsActive = request.IsActive
            };

            var result = await Mediator.Send(command);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireCustomersDeletePermission")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteCustomerCommand { Id = id };
            var result = await Mediator.Send(command);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        [HttpGet("export")]
        [Authorize(Policy = "RequireCustomersExportPermission")]
        public async Task<IActionResult> Export([FromQuery] GetAllCustomersQuery query)
        {
            query.PageSize = int.MaxValue; // Get all for export
            var result = await Mediator.Send(query);

            // Convert to CSV or Excel
            // Implementation depends on your export library
            return File(new byte[0], "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "customers.xlsx");
        }
    }
}
