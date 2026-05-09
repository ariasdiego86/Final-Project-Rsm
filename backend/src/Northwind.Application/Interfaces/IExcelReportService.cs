using Northwind.Domain.Entities;

namespace Northwind.Application.Interfaces;

public interface IExcelReportService
{
    byte[] GenerateOrdersExcel(IEnumerable<Order> orders);
}
