using System.Security.Claims;
using FoodOrderAPI.Data;
using FoodOrderAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly PdfReportService _pdfService;

    public ReportsController(ApplicationDbContext context, PdfReportService pdfService)
    {
        _context = context;
        _pdfService = pdfService;
    }

    // GET: api/Reports/analytics (Admin Dashboard KPIs)
    [Authorize(Roles = "Admin")]
    [HttpGet("analytics")]
    public async Task<IActionResult> GetDashboardAnalytics()
    {
        var totalRevenue = await _context.Orders
            .Where(o => o.Status != "Cancelled")
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

        var totalOrders = await _context.Orders.CountAsync();
        var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending" || o.Status == "Preparing");
        var totalCustomers = await _context.Users.CountAsync(u => u.Role == "Customer");
        var totalMenuItems = await _context.MenuItems.CountAsync();

        var topSellingItems = await _context.OrderItems
            .GroupBy(oi => new { oi.MenuItemId, oi.MenuItem!.Name })
            .Select(g => new
            {
                g.Key.MenuItemId,
                g.Key.Name,
                TotalSold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.Quantity * x.UnitPrice)
            })
            .OrderByDescending(x => x.TotalSold)
            .Take(5)
            .ToListAsync();

        var categorySales = await _context.OrderItems
            .GroupBy(oi => oi.MenuItem!.Category!.Name)
            .Select(g => new
            {
                Category = g.Key,
                Revenue = g.Sum(x => x.Quantity * x.UnitPrice)
            })
            .ToListAsync();

        return Ok(new
        {
            totalRevenue,
            totalOrders,
            pendingOrders,
            totalCustomers,
            totalMenuItems,
            topSellingItems,
            categorySales
        });
    }

    // GET: api/Reports/invoice/{orderId} (Customer or Admin PDF Invoice)
    [HttpGet("invoice/{orderId}")]
    public async Task<IActionResult> DownloadInvoice(int orderId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdmin = User.IsInRole("Admin");

        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Payment)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null) return NotFound(new { message = "Order not found." });
        if (!isAdmin && order.UserId != userId) return Forbid();

        var pdfBytes = _pdfService.GenerateOrderInvoice(order);
        return File(pdfBytes, "application/pdf", $"Invoice_Order_{orderId}.pdf");
    }

    // GET: api/Reports/sales (Admin Sales Summary PDF)
    [Authorize(Roles = "Admin")]
    [HttpGet("sales")]
    public async Task<IActionResult> DownloadSalesReport()
    {
        var orders = await _context.Orders
            .Include(o => o.User)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        var pdfBytes = _pdfService.GenerateSalesReport(orders);
        return File(pdfBytes, "application/pdf", $"Sales_Report_{DateTime.Now:yyyyMMdd}.pdf");
    }
}