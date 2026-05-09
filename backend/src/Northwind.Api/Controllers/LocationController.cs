using Microsoft.AspNetCore.Mvc;
using Northwind.Application.DTOs;
using Northwind.Application.Interfaces;

namespace Northwind.Api.Controllers;

/// <summary>Address validation via Google Maps Address Validation API.</summary>
[ApiController]
[Route("api/[controller]")]
public class LocationController : ControllerBase
{
    private readonly IGeoLocationService _geoService;

    public LocationController(IGeoLocationService geoService) => _geoService = geoService;

    /// <summary>
    /// Validates and standardises a shipping address. Returns formatted address and coordinates.
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(AddressValidationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateAddress(
        [FromBody] AddressValidationRequestDto request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Address))
            return BadRequest(new { detail = "Address is required." });

        var result = await _geoService.ValidateAddressAsync(request.Address, ct);
        return Ok(result);
    }
}
