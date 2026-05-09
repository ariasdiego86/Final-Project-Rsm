using Microsoft.EntityFrameworkCore;
using Northwind.Application.DTOs;
using Northwind.Application.Interfaces;
using Northwind.Domain.Entities;

namespace Northwind.Infrastructure.Persistence.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<Order> Items, int TotalCount)> GetPagedAsync(OrderFilterDto filter, CancellationToken ct = default)
    {
        var query = BuildFilteredQuery(filter);

        var totalCount = await query.CountAsync(ct);

        query = ApplySorting(query, filter.SortBy, filter.SortDir);
        query = query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize);

        var items = await query
            .Include(o => o.Customer)
            .Include(o => o.Employee)
            .Include(o => o.OrderDetails)
            .AsNoTracking()
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<Order?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Employee)
            .Include(o => o.Shipper)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderID == id, ct);
    }

    public async Task<Order> CreateAsync(Order order, CancellationToken ct = default)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync(ct);
        return order;
    }

    public async Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        var existing = await _context.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.OrderID == order.OrderID, ct);

        if (existing is null) return;

        _context.Entry(existing).CurrentValues.SetValues(order);

        _context.OrderDetails.RemoveRange(existing.OrderDetails);
        foreach (var detail in order.OrderDetails)
        {
            _context.OrderDetails.Add(new OrderDetail
            {
                OrderID = existing.OrderID,
                ProductID = detail.ProductID,
                UnitPrice = detail.UnitPrice,
                Quantity = detail.Quantity,
                Discount = detail.Discount
            });
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.OrderID == id, ct);
        if (order is null) return;

        _context.OrderDetails.RemoveRange(order.OrderDetails);
        _context.Orders.Remove(order);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<Order>> GetAllForReportAsync(OrderFilterDto filter, CancellationToken ct = default)
    {
        var query = BuildFilteredQuery(filter);
        return await query
            .Include(o => o.OrderDetails)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    private IQueryable<Order> BuildFilteredQuery(OrderFilterDto filter)
    {
        var query = _context.Orders.AsQueryable();

        if (filter.Year.HasValue)
            query = query.Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Year == filter.Year.Value);

        if (filter.Month.HasValue)
            query = query.Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Month == filter.Month.Value);

        if (filter.Week.HasValue)
        {
            query = query.Where(o => o.OrderDate.HasValue &&
                (o.OrderDate.Value.Day - 1) / 7 + 1 == filter.Week.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Region))
            query = query.Where(o => o.ShipCountry == filter.Region || o.ShipRegion == filter.Region);

        return query;
    }

    private static IQueryable<Order> ApplySorting(IQueryable<Order> query, string sortBy, string sortDir)
    {
        var descending = sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);
        return sortBy.ToLowerInvariant() switch
        {
            "customerid" => descending ? query.OrderByDescending(o => o.CustomerID) : query.OrderBy(o => o.CustomerID),
            "shippeddate" => descending ? query.OrderByDescending(o => o.ShippedDate) : query.OrderBy(o => o.ShippedDate),
            "freight" => descending ? query.OrderByDescending(o => o.Freight) : query.OrderBy(o => o.Freight),
            _ => descending ? query.OrderByDescending(o => o.OrderDate) : query.OrderBy(o => o.OrderDate)
        };
    }
}
