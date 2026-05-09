using FluentAssertions;
using FluentValidation.TestHelper;
using Northwind.Application.DTOs;
using Northwind.Application.Validators;

namespace Northwind.Tests.Application.Validators;

public class CreateOrderDtoValidatorTests
{
    private readonly CreateOrderDtoValidator _sut = new();

    private static CreateOrderDto ValidDto() => new()
    {
        CustomerID = "ALFKI",
        EmployeeID = 1,
        OrderDate = DateTime.UtcNow.AddDays(-1),
        RequiredDate = DateTime.UtcNow.AddDays(7),
        Freight = 10m,
        OrderDetails = [new OrderDetailWriteDto { ProductID = 1, UnitPrice = 18m, Quantity = 2, Discount = 0f }]
    };

    [Fact]
    public async Task Valid_dto_passes_validation()
    {
        var result = await _sut.TestValidateAsync(ValidDto());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Empty_CustomerID_fails()
    {
        var dto = ValidDto();
        dto.CustomerID = string.Empty;
        var result = await _sut.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.CustomerID);
    }

    [Fact]
    public async Task CustomerID_longer_than_5_chars_fails()
    {
        var dto = ValidDto();
        dto.CustomerID = "TOOLONG";
        var result = await _sut.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.CustomerID);
    }

    [Fact]
    public async Task EmployeeID_zero_fails()
    {
        var dto = ValidDto();
        dto.EmployeeID = 0;
        var result = await _sut.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.EmployeeID);
    }

    [Fact]
    public async Task Future_OrderDate_fails()
    {
        var dto = ValidDto();
        dto.OrderDate = DateTime.UtcNow.AddDays(1);
        var result = await _sut.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.OrderDate);
    }

    [Fact]
    public async Task RequiredDate_before_OrderDate_fails()
    {
        var dto = ValidDto();
        dto.OrderDate = DateTime.UtcNow.AddDays(-5);
        dto.RequiredDate = DateTime.UtcNow.AddDays(-10);
        var result = await _sut.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.RequiredDate);
    }

    [Fact]
    public async Task Negative_Freight_fails()
    {
        var dto = ValidDto();
        dto.Freight = -1m;
        var result = await _sut.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Freight);
    }

    [Fact]
    public async Task Empty_OrderDetails_fails()
    {
        var dto = ValidDto();
        dto.OrderDetails = [];
        var result = await _sut.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.OrderDetails);
    }

    [Fact]
    public async Task OrderDetail_with_zero_ProductID_fails()
    {
        var dto = ValidDto();
        dto.OrderDetails = [new OrderDetailWriteDto { ProductID = 0, UnitPrice = 10m, Quantity = 1, Discount = 0f }];
        var result = await _sut.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor("OrderDetails[0].ProductID");
    }

    [Fact]
    public async Task OrderDetail_with_zero_quantity_fails()
    {
        var dto = ValidDto();
        dto.OrderDetails = [new OrderDetailWriteDto { ProductID = 1, UnitPrice = 10m, Quantity = 0, Discount = 0f }];
        var result = await _sut.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor("OrderDetails[0].Quantity");
    }

    [Fact]
    public async Task OrderDetail_with_negative_price_fails()
    {
        var dto = ValidDto();
        dto.OrderDetails = [new OrderDetailWriteDto { ProductID = 1, UnitPrice = -5m, Quantity = 1, Discount = 0f }];
        var result = await _sut.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor("OrderDetails[0].UnitPrice");
    }

    [Fact]
    public async Task OrderDetail_with_discount_above_1_fails()
    {
        var dto = ValidDto();
        dto.OrderDetails = [new OrderDetailWriteDto { ProductID = 1, UnitPrice = 10m, Quantity = 1, Discount = 1.5f }];
        var result = await _sut.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor("OrderDetails[0].Discount");
    }

    [Fact]
    public async Task More_than_100_lines_fails()
    {
        var dto = ValidDto();
        dto.OrderDetails = Enumerable.Range(1, 101)
            .Select(i => new OrderDetailWriteDto { ProductID = i, UnitPrice = 10m, Quantity = 1, Discount = 0f })
            .ToList();
        var result = await _sut.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.OrderDetails);
    }
}
