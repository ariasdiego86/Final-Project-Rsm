using AutoMapper;
using Northwind.Application.DTOs;
using Northwind.Domain.Entities;

namespace Northwind.Application.Mappings;

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        // Entity → Read DTOs
        CreateMap<Order, OrderListItemDto>()
            .ForMember(d => d.CustomerName,
                o => o.MapFrom(s => s.Customer != null ? s.Customer.CompanyName : s.CustomerID))
            .ForMember(d => d.EmployeeName,
                o => o.MapFrom(s => s.Employee != null
                    ? $"{s.Employee.FirstName} {s.Employee.LastName}" : null))
            .ForMember(d => d.Total,
                o => o.MapFrom(s =>
                    s.OrderDetails.Sum(od => od.UnitPrice * od.Quantity * (decimal)(1 - od.Discount))))
            .ForMember(d => d.ProductCount,
                o => o.MapFrom(s => s.OrderDetails.Count));

        CreateMap<Order, OrderDto>()
            .ForMember(d => d.CustomerName,
                o => o.MapFrom(s => s.Customer != null ? s.Customer.CompanyName : s.CustomerID))
            .ForMember(d => d.EmployeeName,
                o => o.MapFrom(s => s.Employee != null
                    ? $"{s.Employee.FirstName} {s.Employee.LastName}" : null))
            .ForMember(d => d.ShipperName,
                o => o.MapFrom(s => s.Shipper != null ? s.Shipper.CompanyName : null));

        CreateMap<OrderDetail, OrderDetailDto>()
            .ForMember(d => d.ProductName,
                o => o.MapFrom(s => s.Product != null ? s.Product.ProductName : null));

        // Write DTOs → Entity
        CreateMap<CreateOrderDto, Order>()
            .ForMember(d => d.OrderID, o => o.Ignore())
            .ForMember(d => d.Customer, o => o.Ignore())
            .ForMember(d => d.Employee, o => o.Ignore())
            .ForMember(d => d.Shipper, o => o.Ignore());

        CreateMap<OrderDetailWriteDto, OrderDetail>()
            .ForMember(d => d.Order, o => o.Ignore())
            .ForMember(d => d.Product, o => o.Ignore())
            .ForMember(d => d.OrderID, o => o.Ignore());
    }
}
