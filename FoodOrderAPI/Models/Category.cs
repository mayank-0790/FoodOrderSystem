using System.ComponentModel.DataAnnotations;

namespace FoodOrderAPI.Models;

public class Category
{
    [Key]
    public int CategoryId { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation property
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}