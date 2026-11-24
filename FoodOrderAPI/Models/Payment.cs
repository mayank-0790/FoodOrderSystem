using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderAPI.Models;

public class Payment
{
    [Key]
    public int PaymentId { get; set; }

    [Required]
    public int OrderId { get; set; }

    [Required, MaxLength(30)]
    public string PaymentMethod { get; set; } = "COD";

    [MaxLength(100)]
    public string? TransactionRef { get; set; }

    [Required, MaxLength(30)]
    public string PaymentStatus { get; set; } = "Pending";

    public DateTime? PaidAt { get; set; }

    [ForeignKey("OrderId")]
    public Order? Order { get; set; }
}

