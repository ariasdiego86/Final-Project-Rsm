using Microsoft.AspNetCore.Mvc;
using Northwind.Application.DTOs;
using Northwind.Application.Services;

namespace Northwind.Api.Controllers;

/// <summary>Orders CRUD + report exports.</summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _service;

    public OrdersController(IOrderService service) => _service = service;

    /// <summary>Returns a paged, filtered list of orders.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<OrderListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders([FromQuery] OrderFilterDto filter, CancellationToken ct)
    {
        var result = await _service.GetOrdersAsync(filter, ct);
        return Ok(result);
    }

    /// <summary>Returns a single order with its details.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(int id, CancellationToken ct)
    {
        var order = await _service.GetOrderByIdAsync(id, ct);
        return Ok(order);
    }

    /// <summary>Creates a new order. Geocodes the shipping address if coordinates are not provided.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto, CancellationToken ct)
    {
        var created = await _service.CreateOrderAsync(dto, ct);
        return CreatedAtAction(nameof(GetOrder), new { id = created.OrderID }, created);
    }

    /// <summary>Updates an existing order.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateOrder(int id, [FromBody] UpdateOrderDto dto, CancellationToken ct)
    {
        var updated = await _service.UpdateOrderAsync(id, dto, ct);
        return Ok(updated);
    }

    /// <summary>Deletes an order and its detail lines.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOrder(int id, CancellationToken ct)
    {
        await _service.DeleteOrderAsync(id, ct);
        return NoContent();
    }

    /// <summary>Downloads a PDF report for a single order.</summary>
    [HttpGet("{id:int}/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderPdf(int id, CancellationToken ct)
    {
        var bytes = await _service.GenerateOrderPdfAsync(id, ct);
        return File(bytes, "application/pdf", $"order-{id}.pdf");
    }

    /// <summary>Downloads an Excel report for the current filtered set of orders.</summary>
    [HttpGet("excel")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrdersExcel([FromQuery] OrderFilterDto filter, CancellationToken ct)
    {
        var bytes = await _service.GenerateOrdersExcelAsync(filter, ct);
        const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        return File(bytes, contentType, $"orders-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }
}
