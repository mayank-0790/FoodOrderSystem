using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderAPI.Models;

public class MenuItem
{
    [Key]
    public int MenuItemId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [MaxLength(300)]
    public string? ImageUrl { get; set; }

    public bool IsAvailable { get; set; } = true;

    public int PrepTimeMinutes { get; set; } = 15;

    // Navigation properties
    [ForeignKey("CategoryId")]
    public Category? Category { get; set; }

    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}