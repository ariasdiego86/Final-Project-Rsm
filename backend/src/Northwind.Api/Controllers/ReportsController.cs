using Microsoft.AspNetCore.Mvc;
using Northwind.Application.DTOs;
using Northwind.Application.Services;

namespace Northwind.Api.Controllers;

/// <summary>Aggregated data endpoints for dashboard charts.</summary>
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IOrderService _service;

    public ReportsController(IOrderService service) => _service = service;

    /// <summary>Returns order counts grouped by time.</summary>
    /// <param name="granularity">Grouping level: <c>month</c> (default), <c>year</c>, or <c>week</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("orders-per-time")]
    [ProducesResponseType(typeof(IEnumerable<TimeSeriesPointDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrdersPerTime(
        [FromQuery] string granularity = "month",
        CancellationToken ct = default)
    {
        var data = await _service.GetOrdersPerTimeAsync(granularity, ct);
        return Ok(data);
    }

    /// <summary>
    /// Returns shipment counts grouped by ship country.
    /// Null or empty ShipCountry is reported as "Sin región".
    /// </summary>
    [HttpGet("shipments-by-region")]
    [ProducesResponseType(typeof(IEnumerable<RegionShipmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShipmentsByRegion(CancellationToken ct)
    {
        var data = await _service.GetShipmentsByRegionAsync(ct);
        return Ok(data);
    }
}
