using System.ComponentModel.DataAnnotations;

namespace PhoneShop.Models;

public sealed class CheckoutViewModel
{
    public List<CartItem> CartItems { get; init; } = [];
    public string PayPalClientId { get; init; } = string.Empty;
    public string Currency { get; init; } = "USD";
    public bool IsPayPalConfigured { get; init; }
}

public sealed class CheckoutCustomerInput
{
    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, StringLength(50)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Message { get; set; }
}

public sealed record PayPalCheckoutDraft(
    string PayPalOrderId,
    string FirstName,
    string LastName,
    string Address,
    string Email,
    string Phone,
    string? Message,
    decimal Total,
    string Currency,
    DateTimeOffset CreatedAt);
