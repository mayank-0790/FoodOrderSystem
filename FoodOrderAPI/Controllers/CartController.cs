using System.Security.Claims;
using FoodOrderAPI.Data;
using FoodOrderAPI.DTOs;
using FoodOrderAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CartController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var userId = GetUserId();
        var items = await _context.CartItems
            .Where(c => c.UserId == userId)
            .Include(c => c.MenuItem)
            .Select(c => new
            {
                c.CartItemId,
                c.MenuItemId,
                Name = c.MenuItem!.Name,
                Price = c.MenuItem.Price,
                ImageUrl = c.MenuItem.ImageUrl,
                c.Quantity,
                Subtotal = c.Quantity * c.MenuItem.Price
            })
            .ToListAsync();

        var total = items.Sum(i => i.Subtotal);
        return Ok(new { items, total });
    }

    [HttpPost]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
    {
        var userId = GetUserId();
        var menuItem = await _context.MenuItems.FindAsync(dto.MenuItemId);
        if (menuItem == null || !menuItem.IsAvailable)
            return BadRequest(new { message = "Item is unavailable." });

        var existing = await _context.CartItems
            .FirstOrDefaultAsync(c => c.UserId == userId && c.MenuItemId == dto.MenuItemId);

        if (existing != null)
        {
            existing.Quantity += dto.Quantity;
        }
        else
        {
            _context.CartItems.Add(new CartItem
            {
                UserId = userId,
                MenuItemId = dto.MenuItemId,
                Quantity = dto.Quantity
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Added to cart." });
    }

    [HttpPut("{cartItemId}")]
    public async Task<IActionResult> UpdateQuantity(int cartItemId, [FromQuery] int quantity)
    {
        var userId = GetUserId();
        var cartItem = await _context.CartItems
            .FirstOrDefaultAsync(c => c.CartItemId == cartItemId && c.UserId == userId);

        if (cartItem == null) return NotFound();

        if (quantity <= 0)
            _context.CartItems.Remove(cartItem);
        else
            cartItem.Quantity = quantity;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Cart updated." });
    }

    [HttpDelete("{cartItemId}")]
    public async Task<IActionResult> RemoveItem(int cartItemId)
    {
        var userId = GetUserId();
        var cartItem = await _context.CartItems
            .FirstOrDefaultAsync(c => c.CartItemId == cartItemId && c.UserId == userId);

        if (cartItem == null) return NotFound();

        _context.CartItems.Remove(cartItem);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Item removed from cart." });
    }
}