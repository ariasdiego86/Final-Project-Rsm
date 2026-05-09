using Northwind.Application.Interfaces;
using Northwind.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Northwind.Infrastructure.Reports;

public class PdfReportService : IPdfReportService
{
    public byte[] GenerateOrderPdf(Order order)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(ComposeHeader);
                page.Content().Element(c => ComposeContent(c, order));
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("Northwind Traders").FontSize(20).Bold();
                col.Item().Text("Order Report").FontSize(12).FontColor(Colors.Grey.Medium);
            });
            row.ConstantItem(100).Height(50).Placeholder();
        });
    }

    private static void ComposeContent(IContainer container, Order order)
    {
        container.Column(col =>
        {
            col.Spacing(10);

            col.Item().Element(c => ComposeOrderMetadata(c, order));
            col.Item().Element(c => ComposeOrderDetails(c, order));
            col.Item().Element(c => ComposeTotals(c, order));
        });
    }

    private static void ComposeOrderMetadata(IContainer container, Order order)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn();
                cols.RelativeColumn();
            });

            table.Cell().Element(CellStyle).Text($"Order #: {order.OrderID}").Bold();
            table.Cell().Element(CellStyle).Text($"Date: {order.OrderDate:yyyy-MM-dd}");
            table.Cell().Element(CellStyle).Text($"Customer: {order.Customer?.CompanyName ?? order.CustomerID}");
            table.Cell().Element(CellStyle).Text($"Employee: {order.Employee?.FirstName} {order.Employee?.LastName}");
            table.Cell().Element(CellStyle).Text($"Shipper: {order.Shipper?.CompanyName}");
            table.Cell().Element(CellStyle).Text($"Freight: {order.Freight:C}");
            table.Cell().ColumnSpan(2).Element(CellStyle).Text($"Ship To: {order.ShipAddress}, {order.ShipCity}, {order.ShipCountry}");
        });
    }

    private static void ComposeOrderDetails(IContainer container, Order order)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(3);
                cols.RelativeColumn();
                cols.RelativeColumn();
                cols.RelativeColumn();
                cols.RelativeColumn();
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderStyle).Text("Product");
                header.Cell().Element(HeaderStyle).AlignRight().Text("Qty");
                header.Cell().Element(HeaderStyle).AlignRight().Text("Unit Price");
                header.Cell().Element(HeaderStyle).AlignRight().Text("Discount");
                header.Cell().Element(HeaderStyle).AlignRight().Text("Subtotal");
            });

            foreach (var detail in order.OrderDetails)
            {
                var subtotal = detail.UnitPrice * detail.Quantity * (decimal)(1 - detail.Discount);
                table.Cell().Element(CellStyle).Text(detail.Product?.ProductName ?? detail.ProductID.ToString());
                table.Cell().Element(CellStyle).AlignRight().Text(detail.Quantity.ToString());
                table.Cell().Element(CellStyle).AlignRight().Text($"{detail.UnitPrice:C}");
                table.Cell().Element(CellStyle).AlignRight().Text($"{detail.Discount:P0}");
                table.Cell().Element(CellStyle).AlignRight().Text($"{subtotal:C}");
            }
        });
    }

    private static void ComposeTotals(IContainer container, Order order)
    {
        var subtotal = order.OrderDetails.Sum(od => od.UnitPrice * od.Quantity * (decimal)(1 - od.Discount));
        var total = subtotal + (order.Freight ?? 0);

        container.AlignRight().Column(col =>
        {
            col.Item().Text($"Subtotal: {subtotal:C}");
            col.Item().Text($"Freight: {order.Freight:C}");
            col.Item().Text($"Total: {total:C}").Bold();
        });
    }

    private static IContainer CellStyle(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4);

    private static IContainer HeaderStyle(IContainer container) =>
        container.Background(Colors.Grey.Lighten3).Padding(4);
}
