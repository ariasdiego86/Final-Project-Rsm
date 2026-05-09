using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Northwind.Application.DTOs;
using Northwind.Tests.Helpers;

namespace Northwind.Tests.Api;

public class NorthwindApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public NorthwindApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        factory.SeedDatabase();
        _client = factory.CreateClient();
    }

    // ── Orders ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_orders_returns_200_with_paged_result()
    {
        var res = await _client.GetAsync("/api/orders");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<PagedResultDto<OrderListItemDto>>();
        body.Should().NotBeNull();
        body!.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GET_orders_with_year_filter_returns_matching_orders()
    {
        var res = await _client.GetAsync("/api/orders?year=1996");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<PagedResultDto<OrderListItemDto>>();
        body!.Items.Should().AllSatisfy(o => o.OrderDate!.Value.Year.Should().Be(1996));
    }

    [Fact]
    public async Task GET_order_by_id_returns_200_with_order()
    {
        var res = await _client.GetAsync("/api/orders/10248");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<OrderDto>();
        body!.OrderID.Should().Be(10248);
        body.OrderDetails.Should().HaveCount(1);
    }

    [Fact]
    public async Task GET_order_by_id_returns_404_for_nonexistent()
    {
        var res = await _client.GetAsync("/api/orders/99999");

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_order_returns_201_with_created_order()
    {
        var dto = new CreateOrderDto
        {
            CustomerID = "ALFKI",
            EmployeeID = 1,
            OrderDate = DateTime.UtcNow.AddDays(-1),
            OrderDetails = [new OrderDetailWriteDto { ProductID = 1, UnitPrice = 18m, Quantity = 2, Discount = 0f }]
        };

        var res = await _client.PostAsJsonAsync("/api/orders", dto);

        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await res.Content.ReadFromJsonAsync<OrderDto>();
        body!.CustomerID.Should().Be("ALFKI");
        body.OrderID.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task POST_order_returns_400_with_ProblemDetails_for_invalid_dto()
    {
        var dto = new CreateOrderDto
        {
            CustomerID = "",
            EmployeeID = 0,
            OrderDetails = []
        };

        var res = await _client.PostAsJsonAsync("/api/orders", dto);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await res.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        body.Should().NotBeNull();
        body!.Title.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PUT_order_returns_200_with_updated_data()
    {
        var dto = new UpdateOrderDto { ShipCountry = "France" };

        var res = await _client.PutAsJsonAsync("/api/orders/10248", dto);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<OrderDto>();
        body!.ShipCountry.Should().Be("France");
    }

    [Fact]
    public async Task PUT_order_returns_404_for_nonexistent()
    {
        var res = await _client.PutAsJsonAsync("/api/orders/99999", new UpdateOrderDto());

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_order_returns_204()
    {
        var res = await _client.DeleteAsync("/api/orders/10250");

        res.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DELETE_order_returns_404_for_nonexistent()
    {
        var res = await _client.DeleteAsync("/api/orders/99999");

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Lookups ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_lookups_customers_returns_200()
    {
        var res = await _client.GetAsync("/api/lookups/customers");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<List<LookupDto>>();
        body.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GET_lookups_employees_returns_200()
    {
        var res = await _client.GetAsync("/api/lookups/employees");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_lookups_products_returns_only_active()
    {
        var res = await _client.GetAsync("/api/lookups/products");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<List<LookupDto>>();
        body.Should().HaveCount(2);
    }

    // ── Reports ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_reports_orders_per_time_returns_200()
    {
        var res = await _client.GetAsync("/api/reports/orders-per-time?granularity=month");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<List<TimeSeriesPointDto>>();
        body.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GET_reports_shipments_by_region_returns_200()
    {
        var res = await _client.GetAsync("/api/reports/shipments-by-region");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<List<RegionShipmentDto>>();
        body.Should().NotBeEmpty();
    }

    // ── Location ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_location_validate_returns_200_with_coordinates()
    {
        var dto = new AddressValidationRequestDto { Address = "Obere Str. 57, Berlin" };

        var res = await _client.PostAsJsonAsync("/api/location/validate", dto);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<AddressValidationResultDto>();
        body!.IsValid.Should().BeTrue();
        body.Latitude.Should().NotBeNull();
    }

    // ── Excel / PDF ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_orders_excel_returns_xlsx_content_type()
    {
        var res = await _client.GetAsync("/api/orders/excel");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        res.Content.Headers.ContentType!.MediaType.Should().Contain("spreadsheetml");
    }

    [Fact]
    public async Task GET_order_pdf_returns_pdf_content_type()
    {
        var res = await _client.GetAsync("/api/orders/10249/pdf");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        res.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
    }
}
