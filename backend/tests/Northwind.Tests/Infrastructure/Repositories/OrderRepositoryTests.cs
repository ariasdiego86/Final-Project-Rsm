using FluentAssertions;
using Northwind.Application.DTOs;
using Northwind.Infrastructure.Persistence.Repositories;
using Northwind.Tests.Helpers;

namespace Northwind.Tests.Infrastructure.Repositories;

public class OrderRepositoryTests
{
    private static OrderRepository CreateRepo(out Northwind.Infrastructure.Persistence.AppDbContext ctx, string? dbName = null)
    {
        ctx = InMemoryDbHelper.CreateContext(dbName);
        SeedData.Seed(ctx);
        return new OrderRepository(ctx);
    }

    [Fact]
    public async Task GetPagedAsync_returns_all_when_no_filters()
    {
        var sut = CreateRepo(out _);
        var filter = new OrderFilterDto { Page = 1, PageSize = 50 };

        var (items, total) = await sut.GetPagedAsync(filter);

        total.Should().Be(3);
        items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetPagedAsync_filters_by_year()
    {
        var sut = CreateRepo(out _);
        var filter = new OrderFilterDto { Year = 1996, Page = 1, PageSize = 50 };

        var (items, total) = await sut.GetPagedAsync(filter);

        total.Should().Be(2);
        items.Should().AllSatisfy(o => o.OrderDate!.Value.Year.Should().Be(1996));
    }

    [Fact]
    public async Task GetPagedAsync_filters_by_year_and_month()
    {
        var sut = CreateRepo(out _);
        var filter = new OrderFilterDto { Year = 1996, Month = 7, Page = 1, PageSize = 50 };

        var (items, total) = await sut.GetPagedAsync(filter);

        total.Should().Be(2);
        items.Should().AllSatisfy(o => o.OrderDate!.Value.Month.Should().Be(7));
    }

    [Fact]
    public async Task GetPagedAsync_filters_by_region()
    {
        var sut = CreateRepo(out _);
        var filter = new OrderFilterDto { Region = "Germany", Page = 1, PageSize = 50 };

        var (items, total) = await sut.GetPagedAsync(filter);

        total.Should().Be(2);
        items.Should().AllSatisfy(o => o.ShipCountry.Should().Be("Germany"));
    }

    [Fact]
    public async Task GetPagedAsync_respects_pagination()
    {
        var sut = CreateRepo(out _);
        var filter = new OrderFilterDto { Page = 1, PageSize = 2, SortBy = "orderDate", SortDir = "asc" };

        var (items, total) = await sut.GetPagedAsync(filter);

        total.Should().Be(3);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_nonexistent()
    {
        var sut = CreateRepo(out _);

        var result = await sut.GetByIdAsync(99999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_returns_order_with_details()
    {
        var sut = CreateRepo(out _);

        var result = await sut.GetByIdAsync(10248);

        result.Should().NotBeNull();
        result!.OrderDetails.Should().HaveCount(1);
        result.Customer.Should().NotBeNull();
        result.Employee.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_persists_new_order()
    {
        var sut = CreateRepo(out var ctx);
        var order = new Northwind.Domain.Entities.Order
        {
            CustomerID = "ALFKI",
            EmployeeID = 1,
            OrderDate = DateTime.UtcNow,
            OrderDetails = new List<Northwind.Domain.Entities.OrderDetail>
            {
                new() { ProductID = 1, UnitPrice = 18m, Quantity = 1, Discount = 0f }
            }
        };

        var created = await sut.CreateAsync(order);

        created.OrderID.Should().BeGreaterThan(0);
        ctx.Orders.Count().Should().Be(4);
    }

    [Fact]
    public async Task DeleteAsync_removes_order_and_details()
    {
        var sut = CreateRepo(out var ctx);

        await sut.DeleteAsync(10248);

        ctx.Orders.Any(o => o.OrderID == 10248).Should().BeFalse();
        ctx.OrderDetails.Any(od => od.OrderID == 10248).Should().BeFalse();
    }
}
