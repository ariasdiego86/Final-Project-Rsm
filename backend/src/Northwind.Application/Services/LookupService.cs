using Northwind.Application.DTOs;
using Northwind.Application.Interfaces;

namespace Northwind.Application.Services;

public class LookupService : ILookupService
{
    private readonly ILookupRepository _repository;

    public LookupService(ILookupRepository repository)
    {
        _repository = repository;
    }

    public Task<IEnumerable<LookupDto>> GetCustomersAsync(CancellationToken ct = default) =>
        _repository.GetCustomersAsync(ct);

    public Task<IEnumerable<LookupDto>> GetEmployeesAsync(CancellationToken ct = default) =>
        _repository.GetEmployeesAsync(ct);

    public Task<IEnumerable<LookupDto>> GetShippersAsync(CancellationToken ct = default) =>
        _repository.GetShippersAsync(ct);

    public Task<IEnumerable<LookupDto>> GetProductsAsync(CancellationToken ct = default) =>
        _repository.GetProductsAsync(ct);

    public Task<IEnumerable<string>> GetRegionsAsync(CancellationToken ct = default) =>
        _repository.GetShipCountriesAsync(ct);
}
