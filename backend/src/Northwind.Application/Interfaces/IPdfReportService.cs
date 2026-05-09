using Northwind.Domain.Entities;

namespace Northwind.Application.Interfaces;

public interface IPdfReportService
{
    byte[] GenerateOrderPdf(Order order);
}
