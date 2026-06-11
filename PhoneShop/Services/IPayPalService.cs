namespace PhoneShop.Services;

public interface IPayPalService
{
    Task<string> CreateOrderAsync(
        decimal total,
        string currency,
        string description,
        CancellationToken cancellationToken);

    Task<PayPalCaptureResult> CaptureOrderAsync(
        string orderId,
        CancellationToken cancellationToken);
}

public sealed record PayPalCaptureResult(
    string Status,
    decimal Amount,
    string Currency,
    string? PayerEmail);
