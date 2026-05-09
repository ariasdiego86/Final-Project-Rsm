using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using Moq;
using Northwind.Application.DTOs;
using Northwind.Application.Interfaces;
using Northwind.Application.Mappings;
using Northwind.Application.Services;
using Northwind.Application.Validators;
using Northwind.Domain.Entities;
using Northwind.Domain.Exceptions;

namespace Northwind.Tests.Application.Services;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _repoMock = new();
    private readonly Mock<IGeoLocationService> _geoMock = new();
    private readonly Mock<IPdfReportService> _pdfMock = new();
    private readonly Mock<IExcelReportService> _excelMock = new();
    private readonly IMapper _mapper;
    private readonly IValidator<CreateOrderDto> _validator = new CreateOrderDtoValidator();
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(OrderProfile)));
        _mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();
        _sut = new OrderService(_repoMock.Object, _geoMock.Object, _pdfMock.Object, _excelMock.Object, _mapper, _validator);
    }

    // ── GetOrderByIdAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderByIdAsync_returns_dto_when_found()
    {
        var order = new Order { OrderID = 1, CustomerID = "ALFKI", OrderDetails = [] };
        _repoMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(order);

        var result = await _sut.GetOrderByIdAsync(1);

        result.OrderID.Should().Be(1);
        result.CustomerID.Should().Be("ALFKI");
    }

    [Fact]
    public async Task GetOrderByIdAsync_throws_NotFoundException_when_not_found()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((Order?)null);

        Func<Task> act = () => _sut.GetOrderByIdAsync(99);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── GetOrdersAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrdersAsync_returns_paged_result()
    {
        var orders = new List<Order>
        {
            new() { OrderID = 1, CustomerID = "ALFKI", OrderDetails = [] },
            new() { OrderID = 2, CustomerID = "ANATR", OrderDetails = [] },
        };
        _repoMock.Setup(r => r.GetPagedAsync(It.IsAny<OrderFilterDto>(), default))
            .ReturnsAsync((orders, 2));

        var filter = new OrderFilterDto { Page = 1, PageSize = 10 };
        var result = await _sut.GetOrdersAsync(filter);

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    // ── CreateOrderAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateOrderAsync_creates_and_returns_dto()
    {
        var dto = ValidCreateDto();
        var savedOrder = new Order { OrderID = 100, CustomerID = dto.CustomerID, OrderDetails = [] };
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Order>(), default)).ReturnsAsync(savedOrder);

        var result = await _sut.CreateOrderAsync(dto);

        result.OrderID.Should().Be(100);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Order>(), default), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_calls_geo_when_address_present_and_no_coords()
    {
        var dto = ValidCreateDto();
        dto.ShipAddress = "Obere Str. 57";
        dto.ShipCity = "Berlin";
        dto.ShipCountry = "Germany";
        dto.Latitude = null;

        var geoResult = new AddressValidationResultDto
        {
            FormattedAddress = "Obere Str. 57, Berlin, Germany",
            Latitude = 52.5m,
            Longitude = 13.4m,
            IsValid = true,
            Issues = []
        };
        _geoMock.Setup(g => g.ValidateAddressAsync(It.IsAny<string>(), default)).ReturnsAsync(geoResult);

        var savedOrder = new Order { OrderID = 101, CustomerID = dto.CustomerID, Latitude = 52.5m, OrderDetails = [] };
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Order>(), default)).ReturnsAsync(savedOrder);

        await _sut.CreateOrderAsync(dto);

        _geoMock.Verify(g => g.ValidateAddressAsync(It.IsAny<string>(), default), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_skips_geo_when_coords_already_provided()
    {
        var dto = ValidCreateDto();
        dto.ShipAddress = "Some Street";
        dto.Latitude = 40.7m;
        dto.Longitude = -74m;

        var savedOrder = new Order { OrderID = 102, CustomerID = dto.CustomerID, OrderDetails = [] };
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Order>(), default)).ReturnsAsync(savedOrder);

        await _sut.CreateOrderAsync(dto);

        _geoMock.Verify(g => g.ValidateAddressAsync(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateOrderAsync_throws_ValidationException_for_invalid_dto()
    {
        var dto = new CreateOrderDto { CustomerID = "", EmployeeID = 0, OrderDetails = [] };

        Func<Task> act = () => _sut.CreateOrderAsync(dto);

        await act.Should().ThrowAsync<ValidationException>();
    }

    // ── UpdateOrderAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateOrderAsync_throws_NotFoundException_when_not_found()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((Order?)null);

        Func<Task> act = () => _sut.UpdateOrderAsync(99, new UpdateOrderDto());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── DeleteOrderAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteOrderAsync_throws_NotFoundException_when_not_found()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((Order?)null);

        Func<Task> act = () => _sut.DeleteOrderAsync(99);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteOrderAsync_calls_repository_delete()
    {
        var order = new Order { OrderID = 5, OrderDetails = [] };
        _repoMock.Setup(r => r.GetByIdAsync(5, default)).ReturnsAsync(order);

        await _sut.DeleteOrderAsync(5);

        _repoMock.Verify(r => r.DeleteAsync(5, default), Times.Once);
    }

    // ── Report helpers ───────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateOrderPdfAsync_throws_NotFoundException_for_missing_order()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((Order?)null);

        Func<Task> act = () => _sut.GenerateOrderPdfAsync(99);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetOrdersPerTimeAsync_returns_monthly_grouping_by_default()
    {
        var orders = new List<Order>
        {
            new() { OrderID = 1, OrderDate = new DateTime(1996, 7, 4), OrderDetails = [] },
            new() { OrderID = 2, OrderDate = new DateTime(1996, 7, 5), OrderDetails = [] },
            new() { OrderID = 3, OrderDate = new DateTime(1996, 8, 1), OrderDetails = [] },
        };
        _repoMock.Setup(r => r.GetAllForReportAsync(It.IsAny<OrderFilterDto>(), default))
            .ReturnsAsync(orders);

        var result = (await _sut.GetOrdersPerTimeAsync("month")).ToList();

        result.Should().HaveCount(2);
        result.First(p => p.Label == "1996-07").Count.Should().Be(2);
        result.First(p => p.Label == "1996-08").Count.Should().Be(1);
    }

    [Fact]
    public async Task GetShipmentsByRegionAsync_groups_null_country_as_sin_region()
    {
        var orders = new List<Order>
        {
            new() { OrderID = 1, ShipCountry = "Germany", OrderDetails = [] },
            new() { OrderID = 2, ShipCountry = null, OrderDetails = [] },
            new() { OrderID = 3, ShipCountry = null, OrderDetails = [] },
        };
        _repoMock.Setup(r => r.GetAllForReportAsync(It.IsAny<OrderFilterDto>(), default))
            .ReturnsAsync(orders);

        var result = (await _sut.GetShipmentsByRegionAsync()).ToList();

        result.Should().HaveCount(2);
        result.First(r => r.Region == "Sin región").Count.Should().Be(2);
    }

    private static CreateOrderDto ValidCreateDto() => new()
    {
        CustomerID = "ALFKI",
        EmployeeID = 1,
        OrderDate = DateTime.UtcNow.AddDays(-1),
        OrderDetails = [new OrderDetailWriteDto { ProductID = 1, UnitPrice = 18m, Quantity = 2, Discount = 0f }]
    };
}
