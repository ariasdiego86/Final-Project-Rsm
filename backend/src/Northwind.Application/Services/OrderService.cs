using AutoMapper;
using FluentValidation;
using Northwind.Application.DTOs;
using Northwind.Application.Interfaces;
using Northwind.Domain.Entities;
using Northwind.Domain.Exceptions;

namespace Northwind.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;
    private readonly IGeoLocationService _geoService;
    private readonly IPdfReportService _pdfService;
    private readonly IExcelReportService _excelService;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateOrderDto> _createValidator;

    public OrderService(
        IOrderRepository repository,
        IGeoLocationService geoService,
        IPdfReportService pdfService,
        IExcelReportService excelService,
        IMapper mapper,
        IValidator<CreateOrderDto> createValidator)
    {
        _repository = repository;
        _geoService = geoService;
        _pdfService = pdfService;
        _excelService = excelService;
        _mapper = mapper;
        _createValidator = createValidator;
    }

    public async Task<PagedResultDto<OrderListItemDto>> GetOrdersAsync(OrderFilterDto filter, CancellationToken ct = default)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(filter, ct);
        return new PagedResultDto<OrderListItemDto>
        {
            Items = _mapper.Map<IEnumerable<OrderListItemDto>>(items),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<OrderDto> GetOrderByIdAsync(int id, CancellationToken ct = default)
    {
        var order = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Order), id);
        return _mapper.Map<OrderDto>(order);
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto, CancellationToken ct = default)
    {
        await _createValidator.ValidateAndThrowAsync(dto, ct);

        var order = _mapper.Map<Order>(dto);
        order.OrderDate ??= DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(dto.ShipAddress) && dto.Latitude is null)
        {
            var geoResult = await TryGeocodeAsync(
                $"{dto.ShipAddress}, {dto.ShipCity}, {dto.ShipCountry}", ct);
            if (geoResult is not null)
            {
                order.Latitude = geoResult.Latitude;
                order.Longitude = geoResult.Longitude;
                order.ShipAddress = geoResult.FormattedAddress;
            }
        }

        var created = await _repository.CreateAsync(order, ct);
        return _mapper.Map<OrderDto>(created);
    }

    public async Task<OrderDto> UpdateOrderAsync(int id, UpdateOrderDto dto, CancellationToken ct = default)
    {
        var existing = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Order), id);

        ApplyUpdateToOrder(existing, dto);

        if (!string.IsNullOrWhiteSpace(existing.ShipAddress) && existing.Latitude is null)
        {
            var geoResult = await TryGeocodeAsync(
                $"{existing.ShipAddress}, {existing.ShipCity}, {existing.ShipCountry}", ct);
            if (geoResult is not null)
            {
                existing.Latitude = geoResult.Latitude;
                existing.Longitude = geoResult.Longitude;
            }
        }

        await _repository.UpdateAsync(existing, ct);

        var updated = await _repository.GetByIdAsync(id, ct) ?? existing;
        return _mapper.Map<OrderDto>(updated);
    }

    public async Task DeleteOrderAsync(int id, CancellationToken ct = default)
    {
        _ = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Order), id);
        await _repository.DeleteAsync(id, ct);
    }

    public async Task<byte[]> GenerateOrderPdfAsync(int id, CancellationToken ct = default)
    {
        var order = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Order), id);
        return _pdfService.GenerateOrderPdf(order);
    }

    public async Task<byte[]> GenerateOrdersExcelAsync(OrderFilterDto filter, CancellationToken ct = default)
    {
        var orders = await _repository.GetAllForReportAsync(filter, ct);
        return _excelService.GenerateOrdersExcel(orders);
    }

    public async Task<IEnumerable<TimeSeriesPointDto>> GetOrdersPerTimeAsync(string granularity, CancellationToken ct = default)
    {
        var filter = new OrderFilterDto { PageSize = int.MaxValue, Page = 1 };
        var orders = (await _repository.GetAllForReportAsync(filter, ct)).ToList();

        return granularity.ToLowerInvariant() switch
        {
            "year" => orders
                .Where(o => o.OrderDate.HasValue)
                .GroupBy(o => o.OrderDate!.Value.Year)
                .OrderBy(g => g.Key)
                .Select(g => new TimeSeriesPointDto { Label = g.Key.ToString(), Count = g.Count() }),

            "week" => orders
                .Where(o => o.OrderDate.HasValue)
                .GroupBy(o => $"{o.OrderDate!.Value.Year}-W{System.Globalization.ISOWeek.GetWeekOfYear(o.OrderDate.Value):D2}")
                .OrderBy(g => g.Key)
                .Select(g => new TimeSeriesPointDto { Label = g.Key, Count = g.Count() }),

            _ => orders
                .Where(o => o.OrderDate.HasValue)
                .GroupBy(o => new { o.OrderDate!.Value.Year, o.OrderDate.Value.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new TimeSeriesPointDto
                {
                    Label = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Count = g.Count()
                })
        };
    }

    public async Task<IEnumerable<RegionShipmentDto>> GetShipmentsByRegionAsync(CancellationToken ct = default)
    {
        var filter = new OrderFilterDto { PageSize = int.MaxValue, Page = 1 };
        var orders = await _repository.GetAllForReportAsync(filter, ct);

        return orders
            .GroupBy(o => string.IsNullOrWhiteSpace(o.ShipCountry) ? "Sin región" : o.ShipCountry)
            .OrderByDescending(g => g.Count())
            .Select(g => new RegionShipmentDto { Region = g.Key, Count = g.Count() });
    }

    private static void ApplyUpdateToOrder(Order existing, UpdateOrderDto dto)
    {
        if (dto.CustomerID is not null) existing.CustomerID = dto.CustomerID;
        if (dto.EmployeeID is not null) existing.EmployeeID = dto.EmployeeID;
        if (dto.ShipVia is not null) existing.ShipVia = dto.ShipVia;
        if (dto.RequiredDate is not null) existing.RequiredDate = dto.RequiredDate;
        if (dto.ShippedDate is not null) existing.ShippedDate = dto.ShippedDate;
        if (dto.Freight is not null) existing.Freight = dto.Freight;
        if (dto.ShipName is not null) existing.ShipName = dto.ShipName;
        if (dto.ShipAddress is not null && existing.ShipAddress != dto.ShipAddress)
        {
            existing.ShipAddress = dto.ShipAddress;
            existing.Latitude = null;
            existing.Longitude = null;
        }
        if (dto.ShipCity is not null) existing.ShipCity = dto.ShipCity;
        if (dto.ShipRegion is not null) existing.ShipRegion = dto.ShipRegion;
        if (dto.ShipPostalCode is not null) existing.ShipPostalCode = dto.ShipPostalCode;
        if (dto.ShipCountry is not null) existing.ShipCountry = dto.ShipCountry;
        if (dto.Latitude is not null) existing.Latitude = dto.Latitude;
        if (dto.Longitude is not null) existing.Longitude = dto.Longitude;

        if (dto.OrderDetails is { Count: > 0 })
        {
            existing.OrderDetails = dto.OrderDetails
                .Select(d => new OrderDetail
                {
                    OrderID = existing.OrderID,
                    ProductID = d.ProductID,
                    UnitPrice = d.UnitPrice,
                    Quantity = d.Quantity,
                    Discount = d.Discount
                }).ToList();
        }
    }

    private async Task<AddressValidationResultDto?> TryGeocodeAsync(string address, CancellationToken ct)
    {
        try
        {
            var result = await _geoService.ValidateAddressAsync(address, ct);
            return result.IsValid ? result : null;
        }
        catch (DomainException)
        {
            return null;
        }
    }
}
