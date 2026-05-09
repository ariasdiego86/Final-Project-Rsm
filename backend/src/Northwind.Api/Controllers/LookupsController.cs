using Microsoft.AspNetCore.Mvc;
using Northwind.Application.DTOs;
using Northwind.Application.Services;

namespace Northwind.Api.Controllers;

/// <summary>Reference data for populating form selects (customers, employees, shippers, products, regions).</summary>
[ApiController]
[Route("api/[controller]")]
public class LookupsController : ControllerBase
{
    private readonly ILookupService _service;

    public LookupsController(ILookupService service) => _service = service;

    /// <summary>Returns all customers as id/name pairs.</summary>
    [HttpGet("customers")]
    [ProducesResponseType(typeof(IEnumerable<LookupDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomers(CancellationToken ct) =>
        Ok(await _service.GetCustomersAsync(ct));

    /// <summary>Returns all employees as id/name pairs.</summary>
    [HttpGet("employees")]
    [ProducesResponseType(typeof(IEnumerable<LookupDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmployees(CancellationToken ct) =>
        Ok(await _service.GetEmployeesAsync(ct));

    /// <summary>Returns all shippers as id/name pairs.</summary>
    [HttpGet("shippers")]
    [ProducesResponseType(typeof(IEnumerable<LookupDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShippers(CancellationToken ct) =>
        Ok(await _service.GetShippersAsync(ct));

    /// <summary>Returns active products as id/name pairs.</summary>
    [HttpGet("products")]
    [ProducesResponseType(typeof(IEnumerable<LookupDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(CancellationToken ct) =>
        Ok(await _service.GetProductsAsync(ct));

    /// <summary>Returns the distinct ship countries present in orders, used as region filter values.</summary>
    [HttpGet("regions")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRegions(CancellationToken ct) =>
        Ok(await _service.GetRegionsAsync(ct));
}
