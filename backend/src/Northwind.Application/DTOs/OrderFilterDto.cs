namespace Northwind.Application.DTOs;

public class OrderFilterDto
{
    public int? Year { get; set; }
    public int? Month { get; set; }
    public int? Week { get; set; }
    public string? Region { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "OrderDate";
    public string SortDir { get; set; } = "desc";
}
