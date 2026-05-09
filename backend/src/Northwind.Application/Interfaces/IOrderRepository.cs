using Northwind.Application.DTOs;
using Northwind.Domain.Entities;

namespace Northwind.Application.Interfaces;

public interface IOrderRepository
{
    Task<(IEnumerable<Order> Items, int TotalCount)> GetPagedAsync(OrderFilterDto filter, CancellationToken ct = default);
    Task<Order?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Order> CreateAsync(Order order, CancellationToken ct = default);
    Task UpdateAsync(Order order, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<Order>> GetAllForReportAsync(OrderFilterDto filter, CancellationToken ct = default);
}
