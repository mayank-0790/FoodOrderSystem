using System.ComponentModel.DataAnnotations;

namespace FoodOrderAPI.DTOs;

public class CategoryDto
{
    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class MenuItemCreateDto
{
    [Required]
    public int CategoryId { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required, Range(1, 100000)]
    public decimal Price { get; set; }

    public bool IsAvailable { get; set; } = true;

    [Range(1, 180)]
    public int PrepTimeMinutes { get; set; } = 15;

    public IFormFile? ImageFile { get; set; }
}

public class AddToCartDto
{
    [Required]
    public int MenuItemId { get; set; }

    [Range(1, 50)]
    public int Quantity { get; set; } = 1;
}

public class CheckoutDto
{
    [Required, MaxLength(300)]
    public string DeliveryAddress { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string PaymentMethod { get; set; } = "COD"; // COD, Card, UPI
}

public class UpdateOrderStatusDto
{
    [Required]
    public string Status { get; set; } = string.Empty; // Pending, Preparing, OutForDelivery, Delivered, Cancelled
}