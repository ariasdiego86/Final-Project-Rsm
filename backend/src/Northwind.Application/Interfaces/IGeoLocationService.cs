using Northwind.Application.DTOs;

namespace Northwind.Application.Interfaces;

public interface IGeoLocationService
{
    Task<AddressValidationResultDto> ValidateAddressAsync(string address, CancellationToken ct = default);
}
