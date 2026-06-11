using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PhoneShop.Configuration;

namespace PhoneShop.Services;

public sealed class PayPalService : IPayPalService
{
    private readonly HttpClient _httpClient;
    private readonly PayPalOptions _options;
    private readonly ILogger<PayPalService> _logger;

    public PayPalService(
        HttpClient httpClient,
        IOptions<PayPalOptions> options,
        ILogger<PayPalService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> CreateOrderAsync(
        decimal total,
        string currency,
        string description,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var accessToken = await GetAccessTokenAsync(cancellationToken);
        var requestBody = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    description,
                    amount = new
                    {
                        currency_code = currency,
                        value = total.ToString("F2", CultureInfo.InvariantCulture)
                    }
                }
            }
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.ApiBaseUrl}/v2/checkout/orders");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("PayPal-Request-Id", Guid.NewGuid().ToString("N"));
        request.Content = JsonContent.Create(requestBody);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        using var document = await ReadResponseAsync(response, cancellationToken);

        if (!document.RootElement.TryGetProperty("id", out var id))
        {
            throw new InvalidOperationException("PayPal did not return an order ID.");
        }

        return id.GetString()
            ?? throw new InvalidOperationException("PayPal returned an empty order ID.");
    }

    public async Task<PayPalCaptureResult> CaptureOrderAsync(
        string orderId,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var accessToken = await GetAccessTokenAsync(cancellationToken);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.ApiBaseUrl}/v2/checkout/orders/{Uri.EscapeDataString(orderId)}/capture");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("PayPal-Request-Id", $"capture-{orderId}");
        request.Content = JsonContent.Create(new { });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        using var document = await ReadResponseAsync(response, cancellationToken);
        var root = document.RootElement;
        var status = root.GetProperty("status").GetString() ?? string.Empty;
        var capture = root
            .GetProperty("purchase_units")[0]
            .GetProperty("payments")
            .GetProperty("captures")[0];
        var amount = capture.GetProperty("amount");
        var amountValue = decimal.Parse(
            amount.GetProperty("value").GetString() ?? "0",
            CultureInfo.InvariantCulture);
        var currency = amount.GetProperty("currency_code").GetString() ?? string.Empty;
        var payerEmail = root.TryGetProperty("payer", out var payer) &&
                         payer.TryGetProperty("email_address", out var email)
            ? email.GetString()
            : null;

        return new PayPalCaptureResult(status, amountValue, currency, payerEmail);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.ApiBaseUrl}/v1/oauth2/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(
            new Dictionary<string, string> { ["grant_type"] = "client_credentials" });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        using var document = await ReadResponseAsync(response, cancellationToken);

        return document.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("PayPal did not return an access token.");
    }

    private async Task<JsonDocument> ReadResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "PayPal API request failed with status {StatusCode}: {ResponseBody}",
                (int)response.StatusCode,
                body);
            throw new InvalidOperationException(
                $"PayPal request failed with status {(int)response.StatusCode}.");
        }

        return JsonDocument.Parse(body);
    }

    private void EnsureConfigured()
    {
        if (!_options.IsConfigured)
        {
            throw new InvalidOperationException(
                "PayPal is not configured. Set PayPal:ClientId and PayPal:ClientSecret.");
        }
    }
}
