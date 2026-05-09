using Northwind.Application.DTOs;

namespace Northwind.Application.Interfaces;

public interface ILookupRepository
{
    Task<IEnumerable<LookupDto>> GetCustomersAsync(CancellationToken ct = default);
    Task<IEnumerable<LookupDto>> GetEmployeesAsync(CancellationToken ct = default);
    Task<IEnumerable<LookupDto>> GetShippersAsync(CancellationToken ct = default);
    Task<IEnumerable<LookupDto>> GetProductsAsync(CancellationToken ct = default);
    Task<IEnumerable<string>> GetShipCountriesAsync(CancellationToken ct = default);
}
