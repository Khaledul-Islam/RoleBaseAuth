using FluentValidation;
using RoleBaseAuth.Application.DTOs.Customers;

namespace RoleBaseAuth.Application.Validators.Customers;

public class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Customer ID is required");

        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required")
            .MaximumLength(200);

        RuleFor(x => x.ContactName)
            .NotEmpty().WithMessage("Contact name is required")
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(100);

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0).WithMessage("Credit limit cannot be negative");
    }
}