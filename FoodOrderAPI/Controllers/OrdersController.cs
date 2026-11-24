using System.Security.Claims;
using FoodOrderAPI.Data;
using FoodOrderAPI.DTOs;
using FoodOrderAPI.Models;
using FoodOrderAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly EmailService _emailService;

    public OrdersController(ApplicationDbContext context, EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto)
    {
        var userId = GetUserId();
        var user = await _context.Users.FindAsync(userId);
        var cartItems = await _context.CartItems
            .Where(c => c.UserId == userId)
            .Include(c => c.MenuItem)
            .ToListAsync();

        if (!cartItems.Any())
            return BadRequest(new { message = "Your cart is empty." });

        var totalAmount = cartItems.Sum(c => c.Quantity * c.MenuItem!.Price);

        var order = new Order
        {
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            TotalAmount = totalAmount,
            Status = "Pending",
            DeliveryAddress = dto.DeliveryAddress.Trim(),
            OrderItems = cartItems.Select(c => new OrderItem
            {
                MenuItemId = c.MenuItemId,
                Quantity = c.Quantity,
                UnitPrice = c.MenuItem!.Price
            }).ToList(),
            Payment = new Payment
            {
                PaymentMethod = dto.PaymentMethod,
                TransactionRef = $"TXN-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                PaymentStatus = dto.PaymentMethod == "COD" ? "Pending" : "Completed",
                PaidAt = dto.PaymentMethod == "COD" ? null : DateTime.UtcNow
            }
        };

        _context.Orders.Add(order);
        _context.CartItems.RemoveRange(cartItems);
        await _context.SaveChangesAsync();

        if (user != null)
        {
            await _emailService.SendOrderConfirmationAsync(
                user.Email, user.FullName, order.OrderId, order.TotalAmount, order.DeliveryAddress);
        }

        return Ok(new
        {
            message = "Order placed successfully!",
            orderId = order.OrderId,
            totalAmount = order.TotalAmount,
            transactionRef = order.Payment.TransactionRef
        });
    }

    [HttpGet("my-orders")]
    public async Task<IActionResult> GetMyOrders()
    {
        var userId = GetUserId();
        var orders = await _context.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new
            {
                o.OrderId,
                o.OrderDate,
                o.TotalAmount,
                o.Status,
                o.DeliveryAddress,
                PaymentMethod = o.Payment != null ? o.Payment.PaymentMethod : "N/A",
                PaymentStatus = o.Payment != null ? o.Payment.PaymentStatus : "Pending",
                Items = o.OrderItems.Select(oi => new
                {
                    oi.MenuItem!.Name,
                    oi.Quantity,
                    oi.UnitPrice,
                    Subtotal = oi.Quantity * oi.UnitPrice
                })
            })
            .ToListAsync();

        return Ok(orders);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAllOrders([FromQuery] string? status)
    {
        var query = _context.Orders.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);

        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new
            {
                o.OrderId,
                CustomerName = o.User!.FullName,
                CustomerEmail = o.User.Email,
                o.OrderDate,
                o.TotalAmount,
                o.Status,
                o.DeliveryAddress,
                PaymentMethod = o.Payment != null ? o.Payment.PaymentMethod : "N/A",
                PaymentStatus = o.Payment != null ? o.Payment.PaymentStatus : "Pending",
                Items = o.OrderItems.Select(oi => new
                {
                    oi.MenuItem!.Name,
                    oi.Quantity,
                    oi.UnitPrice
                })
            })
            .ToListAsync();

        return Ok(orders);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
    {
        var validStatuses = new[] { "Pending", "Preparing", "OutForDelivery", "Delivered", "Cancelled" };
        if (!validStatuses.Contains(dto.Status))
            return BadRequest(new { message = "Invalid order status." });

        var order = await _context.Orders.Include(o => o.Payment).FirstOrDefaultAsync(o => o.OrderId == id);
        if (order == null) return NotFound(new { message = "Order not found." });

        order.Status = dto.Status;
        if (dto.Status == "Delivered" && order.Payment != null)
        {
            order.Payment.PaymentStatus = "Completed";
            order.Payment.PaidAt ??= DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = $"Order #{id} status updated to {dto.Status}." });
    }
}