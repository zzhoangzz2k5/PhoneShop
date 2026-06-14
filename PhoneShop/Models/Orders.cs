using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhoneShop.Models;

[Table("Orders")]
public class Orders
{
    [Key]
    public int Id { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime OrderDate { get; set; }

    [Required, Column(TypeName = "nvarchar(200)")]
    public string CustomerName { get; set; } = string.Empty;

    [Required, Column(TypeName = "nvarchar(200)")]
    public string ShippingPhone { get; set; } = string.Empty;

    [Required, Column(TypeName = "nvarchar(300)")]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required, Column(TypeName = "nvarchar(200)")]
    public string Email { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(1000)")]
    public string? Note { get; set; }

    [Column(TypeName = "money")]
    public decimal TotalAmount { get; set; }

    public int Status { get; set; } = (int)OrderStatus.Processing;

    [Required, Column(TypeName = "nvarchar(50)")]
    public string PaymentMethod { get; set; } = "PayPal";

    [Required, Column(TypeName = "nvarchar(50)")]
    public string PaymentStatus { get; set; } = "Paid";

    [Required, Column(TypeName = "nvarchar(100)")]
    public string PayPalOrderId { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(100)")]
    public string? PayPalCaptureId { get; set; }

    public ICollection<OrdersDetails> OrderDetails { get; set; } = [];
}

public enum OrderStatus
{
    Pending,
    Processing,
    Shipping,
    Completed,
    Cancelled
}
