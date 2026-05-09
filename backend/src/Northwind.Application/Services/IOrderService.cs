using Northwind.Application.DTOs;

namespace Northwind.Application.Services;

public interface IOrderService
{
    Task<PagedResultDto<OrderListItemDto>> GetOrdersAsync(OrderFilterDto filter, CancellationToken ct = default);
    Task<OrderDto> GetOrderByIdAsync(int id, CancellationToken ct = default);
    Task<OrderDto> CreateOrderAsync(CreateOrderDto dto, CancellationToken ct = default);
    Task<OrderDto> UpdateOrderAsync(int id, UpdateOrderDto dto, CancellationToken ct = default);
    Task DeleteOrderAsync(int id, CancellationToken ct = default);
    Task<byte[]> GenerateOrderPdfAsync(int id, CancellationToken ct = default);
    Task<byte[]> GenerateOrdersExcelAsync(OrderFilterDto filter, CancellationToken ct = default);
    Task<IEnumerable<TimeSeriesPointDto>> GetOrdersPerTimeAsync(string granularity, CancellationToken ct = default);
    Task<IEnumerable<RegionShipmentDto>> GetShipmentsByRegionAsync(CancellationToken ct = default);
}
