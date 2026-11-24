using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderAPI.Models;

public class Order
{
    [Key]
    public int OrderId { get; set; }

    [Required]
    public int UserId { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = "Pending"; // Pending, Preparing, OutForDelivery, Delivered, Cancelled

    [Required, MaxLength(300)]
    public string DeliveryAddress { get; set; } = string.Empty;

    // Navigation properties
    [ForeignKey("UserId")]
    public User? User { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public Payment? Payment { get; set; }
}