using FluentValidation;
using Northwind.Application.DTOs;

namespace Northwind.Application.Validators;

public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderDtoValidator()
    {
        RuleFor(x => x.CustomerID)
            .NotEmpty().WithMessage("Customer is required.")
            .MaximumLength(5);

        RuleFor(x => x.EmployeeID)
            .GreaterThan(0).WithMessage("A valid employee must be assigned.");

        RuleFor(x => x.OrderDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .When(x => x.OrderDate.HasValue)
            .WithMessage("Order date cannot be in the future.");

        RuleFor(x => x.RequiredDate)
            .GreaterThanOrEqualTo(x => x.OrderDate ?? DateTime.UtcNow)
            .When(x => x.RequiredDate.HasValue)
            .WithMessage("Required date must be on or after the order date.");

        RuleFor(x => x.Freight)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Freight.HasValue)
            .WithMessage("Freight cannot be negative.");

        RuleFor(x => x.OrderDetails)
            .NotEmpty().WithMessage("At least one order line is required.")
            .Must(lines => lines.Count <= 100)
            .WithMessage("An order cannot have more than 100 lines.");

        RuleForEach(x => x.OrderDetails)
            .SetValidator(new OrderDetailWriteDtoValidator());
    }
}

public class OrderDetailWriteDtoValidator : AbstractValidator<OrderDetailWriteDto>
{
    public OrderDetailWriteDtoValidator()
    {
        RuleFor(x => x.ProductID)
            .GreaterThan(0).WithMessage("A valid product must be selected.");

        RuleFor(x => x.Quantity)
            .GreaterThan((short)0).WithMessage("Quantity must be at least 1.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative.");

        RuleFor(x => x.Discount)
            .InclusiveBetween(0f, 1f).WithMessage("Discount must be between 0 and 1 (0% to 100%).");
    }
}
