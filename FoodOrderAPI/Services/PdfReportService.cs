using FoodOrderAPI.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FoodOrderAPI.Services;

public class PdfReportService
{
    public byte[] GenerateOrderInvoice(Order order)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(35);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("FOODORDER RESTAURANT").FontSize(20).Bold().FontColor(Colors.DeepOrange.Medium);
                        col.Item().Text("Tax Invoice & Order Receipt").FontSize(12).FontColor(Colors.Grey.Darken1);
                    });

                    row.ConstantItem(180).AlignRight().Column(col =>
                    {
                        col.Item().Text($"Invoice #{order.OrderId}").FontSize(14).Bold();
                        col.Item().Text($"Date: {order.OrderDate:dd MMM yyyy HH:mm}");
                        col.Item().Text($"Status: {order.Status}").Bold();
                    });
                });

                page.Content().PaddingVertical(20).Column(col =>
                {
                    col.Item().Background(Colors.Grey.Lighten4).Padding(12).Column(info =>
                    {
                        info.Item().Text($"Customer: {order.User?.FullName ?? "Customer"}").Bold();
                        info.Item().Text($"Email: {order.User?.Email ?? "N/A"}");
                        info.Item().Text($"Delivery Address: {order.DeliveryAddress}");
                        info.Item().Text($"Payment: {order.Payment?.PaymentMethod ?? "N/A"} ({order.Payment?.PaymentStatus ?? "Pending"}) — Ref: {order.Payment?.TransactionRef ?? "N/A"}");
                    });

                    col.Item().PaddingTop(20).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(35);
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.DeepOrange.Medium).Padding(6).Text("#").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.DeepOrange.Medium).Padding(6).Text("Item").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.DeepOrange.Medium).Padding(6).AlignRight().Text("Qty").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.DeepOrange.Medium).Padding(6).AlignRight().Text("Unit Price").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.DeepOrange.Medium).Padding(6).AlignRight().Text("Subtotal").FontColor(Colors.White).Bold();
                        });

                        int idx = 1;
                        foreach (var item in order.OrderItems)
                        {
                            var bg = idx % 2 == 0 ? Colors.Grey.Lighten5 : Colors.White;
                            table.Cell().Background(bg).Padding(6).Text(idx.ToString());
                            table.Cell().Background(bg).Padding(6).Text(item.MenuItem?.Name ?? $"Item #{item.MenuItemId}");
                            table.Cell().Background(bg).Padding(6).AlignRight().Text(item.Quantity.ToString());
                            table.Cell().Background(bg).Padding(6).AlignRight().Text($"₹{item.UnitPrice:F2}");
                            table.Cell().Background(bg).Padding(6).AlignRight().Text($"₹{item.Quantity * item.UnitPrice:F2}");
                            idx++;
                        }
                    });

                    col.Item().PaddingTop(15).AlignRight().Text($"Grand Total: ₹{order.TotalAmount:F2}")
                        .FontSize(15).Bold().FontColor(Colors.DeepOrange.Darken2);
                });

                page.Footer().AlignCenter().Text("Thank you for ordering with FoodOrder Restaurant!").FontSize(10).FontColor(Colors.Grey.Medium);
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateSalesReport(List<Order> orders)
    {
        var totalRevenue = orders.Where(o => o.Status != "Cancelled").Sum(o => o.TotalAmount);
        var totalOrders = orders.Count;
        var deliveredCount = orders.Count(o => o.Status == "Delivered");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(35);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("FOODORDER RESTAURANT — SALES REPORT").FontSize(18).Bold().FontColor(Colors.DeepOrange.Medium);
                        col.Item().Text($"Generated on {DateTime.Now:dd MMM yyyy HH:mm}").FontColor(Colors.Grey.Darken1);
                    });
                });

                page.Content().PaddingVertical(18).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
                        {
                            c.Item().Text("Total Orders").FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text(totalOrders.ToString()).FontSize(16).Bold();
                        });
                        row.ConstantItem(12);
                        row.RelativeItem().Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
                        {
                            c.Item().Text("Delivered Orders").FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text(deliveredCount.ToString()).FontSize(16).Bold();
                        });
                        row.ConstantItem(12);
                        row.RelativeItem().Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
                        {
                            c.Item().Text("Net Revenue").FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"₹{totalRevenue:F2}").FontSize(16).Bold().FontColor(Colors.Green.Darken2);
                        });
                    });

                    col.Item().PaddingTop(18).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(55);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("Order #").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("Date").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("Customer").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("Status").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Grey.Darken3).Padding(6).AlignRight().Text("Amount").FontColor(Colors.White).Bold();
                        });

                        foreach (var o in orders)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text($"#{o.OrderId}");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text($"{o.OrderDate:dd MMM yyyy}");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text(o.User?.FullName ?? "Customer");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text(o.Status);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).AlignRight().Text($"₹{o.TotalAmount:F2}");
                        }
                    });
                });

                page.Footer().AlignCenter().Text("Confidential — Internal Management Report").FontSize(9).FontColor(Colors.Grey.Medium);
            });
        });

        return document.GeneratePdf();
    }
}