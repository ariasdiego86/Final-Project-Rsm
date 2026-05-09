using Microsoft.EntityFrameworkCore;
using Northwind.Application.DTOs;
using Northwind.Application.Interfaces;

namespace Northwind.Infrastructure.Persistence.Repositories;

public class LookupRepository : ILookupRepository
{
    private readonly AppDbContext _context;

    public LookupRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<LookupDto>> GetCustomersAsync(CancellationToken ct = default) =>
        await _context.Customers
            .AsNoTracking()
            .OrderBy(c => c.CompanyName)
            .Select(c => new LookupDto { Id = c.CustomerID, Name = c.CompanyName })
            .ToListAsync(ct);

    public async Task<IEnumerable<LookupDto>> GetEmployeesAsync(CancellationToken ct = default) =>
        await _context.Employees
            .AsNoTracking()
            .OrderBy(e => e.LastName)
            .Select(e => new LookupDto
            {
                Id = e.EmployeeID.ToString(),
                Name = e.FirstName + " " + e.LastName
            })
            .ToListAsync(ct);

    public async Task<IEnumerable<LookupDto>> GetShippersAsync(CancellationToken ct = default) =>
        await _context.Shippers
            .AsNoTracking()
            .OrderBy(s => s.CompanyName)
            .Select(s => new LookupDto { Id = s.ShipperID.ToString(), Name = s.CompanyName })
            .ToListAsync(ct);

    public async Task<IEnumerable<LookupDto>> GetProductsAsync(CancellationToken ct = default) =>
        await _context.Products
            .AsNoTracking()
            .Where(p => !p.Discontinued)
            .OrderBy(p => p.ProductName)
            .Select(p => new LookupDto { Id = p.ProductID.ToString(), Name = p.ProductName })
            .ToListAsync(ct);

    public async Task<IEnumerable<string>> GetShipCountriesAsync(CancellationToken ct = default) =>
        await _context.Orders
            .AsNoTracking()
            .Where(o => o.ShipCountry != null)
            .Select(o => o.ShipCountry!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(ct);
}
