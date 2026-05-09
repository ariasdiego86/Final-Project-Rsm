using ClosedXML.Excel;
using Northwind.Application.Interfaces;
using Northwind.Domain.Entities;

namespace Northwind.Infrastructure.Reports;

public class ExcelReportService : IExcelReportService
{
    public byte[] GenerateOrdersExcel(IEnumerable<Order> orders)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Orders");

        var headers = new[]
        {
            "Order ID", "Customer", "Employee", "Order Date", "Shipped Date",
            "Ship Country", "Ship Region", "Freight", "Total", "Products"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            cell.Style.Font.FontColor = XLColor.White;
        }

        var row = 2;
        foreach (var order in orders)
        {
            var total = order.OrderDetails.Sum(od => od.UnitPrice * od.Quantity * (decimal)(1 - od.Discount));
            var productCount = order.OrderDetails.Count;

            ws.Cell(row, 1).Value = order.OrderID;
            ws.Cell(row, 2).Value = order.Customer?.CompanyName ?? order.CustomerID ?? "";
            ws.Cell(row, 3).Value = order.Employee is not null
                ? $"{order.Employee.FirstName} {order.Employee.LastName}" : "";
            ws.Cell(row, 4).Value = order.OrderDate?.ToString("yyyy-MM-dd") ?? "";
            ws.Cell(row, 5).Value = order.ShippedDate?.ToString("yyyy-MM-dd") ?? "";
            ws.Cell(row, 6).Value = order.ShipCountry ?? "";
            ws.Cell(row, 7).Value = order.ShipRegion ?? "Sin región";
            ws.Cell(row, 8).Value = order.Freight ?? 0;
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 9).Value = total;
            ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 10).Value = productCount;

            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
