using FoodOrderAPI.Data;
using FoodOrderAPI.DTOs;
using FoodOrderAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenuItemsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public MenuItemsController(ApplicationDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    // GET: api/MenuItems?search=paneer&categoryId=2&minPrice=100&maxPrice=500&sortBy=price_asc
    [HttpGet]
    public async Task<IActionResult> GetMenuItems(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] bool? availableOnly,
        [FromQuery] string? sortBy)
    {
        var query = _context.MenuItems.Include(m => m.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower().Trim();
            query = query.Where(m => m.Name.ToLower().Contains(term) ||
                                     (m.Description != null && m.Description.ToLower().Contains(term)));
        }

        if (categoryId.HasValue)
            query = query.Where(m => m.CategoryId == categoryId.Value);

        if (minPrice.HasValue)
            query = query.Where(m => m.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(m => m.Price <= maxPrice.Value);

        if (availableOnly == true)
            query = query.Where(m => m.IsAvailable);

        query = sortBy switch
        {
            "price_asc" => query.OrderBy(m => m.Price),
            "price_desc" => query.OrderByDescending(m => m.Price),
            "name_desc" => query.OrderByDescending(m => m.Name),
            _ => query.OrderBy(m => m.Name)
        };

        var items = await query.Select(m => new
        {
            m.MenuItemId,
            m.CategoryId,
            CategoryName = m.Category != null ? m.Category.Name : "Uncategorized",
            m.Name,
            m.Description,
            m.Price,
            m.ImageUrl,
            m.IsAvailable,
            m.PrepTimeMinutes
        }).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _context.MenuItems
            .Include(m => m.Category)
            .FirstOrDefaultAsync(m => m.MenuItemId == id);

        if (item == null) return NotFound(new { message = "Menu item not found." });
        return Ok(item);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] MenuItemCreateDto dto)
    {
        if (!await _context.Categories.AnyAsync(c => c.CategoryId == dto.CategoryId))
            return BadRequest(new { message = "Invalid CategoryId." });

        string? imageUrl = null;
        if (dto.ImageFile != null && dto.ImageFile.Length > 0)
        {
            imageUrl = await SaveImageAsync(dto.ImageFile);
        }

        var item = new MenuItem
        {
            CategoryId = dto.CategoryId,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            Price = dto.Price,
            ImageUrl = imageUrl,
            IsAvailable = dto.IsAvailable,
            PrepTimeMinutes = dto.PrepTimeMinutes
        };

        _context.MenuItems.Add(item);
        await _context.SaveChangesAsync();
        return Ok(item);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromForm] MenuItemCreateDto dto)
    {
        var item = await _context.MenuItems.FindAsync(id);
        if (item == null) return NotFound(new { message = "Menu item not found." });

        item.CategoryId = dto.CategoryId;
        item.Name = dto.Name.Trim();
        item.Description = dto.Description?.Trim();
        item.Price = dto.Price;
        item.IsAvailable = dto.IsAvailable;
        item.PrepTimeMinutes = dto.PrepTimeMinutes;

        if (dto.ImageFile != null && dto.ImageFile.Length > 0)
        {
            item.ImageUrl = await SaveImageAsync(dto.ImageFile);
        }

        await _context.SaveChangesAsync();
        return Ok(item);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.MenuItems.FindAsync(id);
        if (item == null) return NotFound(new { message = "Menu item not found." });

        _context.MenuItems.Remove(item);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Menu item deleted." });
    }

    private async Task<string> SaveImageAsync(IFormFile file)
    {
        var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");
        Directory.CreateDirectory(uploadsFolder);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/{fileName}";
    }
}