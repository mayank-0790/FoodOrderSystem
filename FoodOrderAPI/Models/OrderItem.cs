using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderAPI.Models;

public class OrderItem
{
    [Key]
    public int OrderItemId { get; set; }

    [Required]
    public int OrderId { get; set; }

    [Required]
    public int MenuItemId { get; set; }

    [Range(1, 100)]
    public int Quantity { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    [ForeignKey("OrderId")]
    public Order? Order { get; set; }

    [ForeignKey("MenuItemId")]
    public MenuItem? MenuItem { get; set; }
}