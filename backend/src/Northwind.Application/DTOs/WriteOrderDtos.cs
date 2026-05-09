namespace Northwind.Application.DTOs;

public class CreateOrderDto
{
    public string CustomerID { get; set; } = string.Empty;
    public int EmployeeID { get; set; }
    public int? ShipVia { get; set; }
    public DateTime? OrderDate { get; set; }
    public DateTime? RequiredDate { get; set; }
    public decimal? Freight { get; set; }
    public string? ShipName { get; set; }
    public string? ShipAddress { get; set; }
    public string? ShipCity { get; set; }
    public string? ShipRegion { get; set; }
    public string? ShipPostalCode { get; set; }
    public string? ShipCountry { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public ICollection<OrderDetailWriteDto> OrderDetails { get; set; } = new List<OrderDetailWriteDto>();
}

public class UpdateOrderDto
{
    public string? CustomerID { get; set; }
    public int? EmployeeID { get; set; }
    public int? ShipVia { get; set; }
    public DateTime? RequiredDate { get; set; }
    public DateTime? ShippedDate { get; set; }
    public decimal? Freight { get; set; }
    public string? ShipName { get; set; }
    public string? ShipAddress { get; set; }
    public string? ShipCity { get; set; }
    public string? ShipRegion { get; set; }
    public string? ShipPostalCode { get; set; }
    public string? ShipCountry { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public ICollection<OrderDetailWriteDto>? OrderDetails { get; set; }
}

public class OrderDetailWriteDto
{
    public int ProductID { get; set; }
    public decimal UnitPrice { get; set; }
    public short Quantity { get; set; }
    public float Discount { get; set; }
}
