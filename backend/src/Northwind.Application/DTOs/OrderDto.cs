namespace Northwind.Application.DTOs;

public class OrderListItemDto
{
    public int OrderID { get; set; }
    public string? CustomerID { get; set; }
    public string? CustomerName { get; set; }
    public string? EmployeeName { get; set; }
    public DateTime? OrderDate { get; set; }
    public DateTime? ShippedDate { get; set; }
    public string? ShipCountry { get; set; }
    public string? ShipRegion { get; set; }
    public decimal? Freight { get; set; }
    public decimal Total { get; set; }
    public int ProductCount { get; set; }
}

public class OrderDto
{
    public int OrderID { get; set; }
    public string? CustomerID { get; set; }
    public string? CustomerName { get; set; }
    public int? EmployeeID { get; set; }
    public string? EmployeeName { get; set; }
    public int? ShipVia { get; set; }
    public string? ShipperName { get; set; }
    public DateTime? OrderDate { get; set; }
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
    public ICollection<OrderDetailDto> OrderDetails { get; set; } = new List<OrderDetailDto>();
}

public class OrderDetailDto
{
    public int OrderID { get; set; }
    public int ProductID { get; set; }
    public string? ProductName { get; set; }
    public decimal UnitPrice { get; set; }
    public short Quantity { get; set; }
    public float Discount { get; set; }
    public decimal Subtotal => UnitPrice * Quantity * (decimal)(1 - Discount);
}
