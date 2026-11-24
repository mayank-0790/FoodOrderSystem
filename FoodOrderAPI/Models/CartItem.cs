using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderAPI.Models;

public class CartItem
{
    [Key]
    public int CartItemId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int MenuItemId { get; set; }

    [Range(1, 50)]
    public int Quantity { get; set; } = 1;

    [ForeignKey("UserId")]
    public User? User { get; set; }

    [ForeignKey("MenuItemId")]
    public MenuItem? MenuItem { get; set; }
}