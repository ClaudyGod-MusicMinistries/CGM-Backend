using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Domain.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClaudyGod.Infrastructure.Services;

public class PaystackService : IPaystackService
{
    private readonly HttpClient _http;
    private readonly string? _secretKey;   // null/empty = not configured, returns 503
    private readonly ILogger<PaystackService> _logger;

    public bool IsConfigured => _secretKey is not null;

    public PaystackService(HttpClient http, IConfiguration config, ILogger<PaystackService> logger)
    {
        _http = http;
        _logger = logger;

        var key = config["Paystack:SecretKey"];
        _secretKey = string.IsNullOrWhiteSpace(key) || key.StartsWith("CHANGE-ME", StringComparison.OrdinalIgnoreCase)
            ? null
            : key;

        if (_secretKey is null)
            _logger.LogWarning("Paystack:SecretKey is not configured. Paystack payment features will return 503 until it's set.");
    }

    public async Task<PaystackVerificationResult> VerifyTransactionAsync(string reference, CancellationToken ct = default)
    {
        if (_secretKey is null)
            throw new ServiceUnavailableException("Paystack",
                "The payment gateway is still being configured and isn't available yet. Please try another payment method or check back soon.");

        using var req = new HttpRequestMessage(HttpMethod.Get,
            $"https://api.paystack.co/transaction/verify/{Uri.EscapeDataString(reference)}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);

        HttpResponseMessage res;
        try
        {
            res = await _http.SendAsync(req, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Paystack verify call failed for reference {Reference}", reference);
            throw new ServiceUnavailableException("Paystack", "Could not reach Paystack to verify this payment. Please try again.");
        }

        var body = await res.Content.ReadFromJsonAsync<PaystackVerifyResponse>(cancellationToken: ct);

        if (!res.IsSuccessStatusCode || body?.Data is null)
        {
            _logger.LogWarning("Paystack verify non-success for {Reference}: HTTP {Status}", reference, (int)res.StatusCode);
            return new PaystackVerificationResult(false, "unknown", 0, string.Empty, reference);
        }

        return new PaystackVerificationResult(
            Success: body.Status && body.Data.Status == "success",
            Status: body.Data.Status,
            AmountInMinorUnits: body.Data.Amount,
            Currency: body.Data.Currency,
            Reference: body.Data.Reference);
    }

    private sealed class PaystackVerifyResponse
    {
        [JsonPropertyName("status")]
        public bool Status { get; init; }

        [JsonPropertyName("data")]
        public PaystackVerifyData? Data { get; init; }
    }

    private sealed class PaystackVerifyData
    {
        [JsonPropertyName("status")]
        public string Status { get; init; } = string.Empty;

        [JsonPropertyName("amount")]
        public long Amount { get; init; }

        [JsonPropertyName("currency")]
        public string Currency { get; init; } = string.Empty;

        [JsonPropertyName("reference")]
        public string Reference { get; init; } = string.Empty;
    }
}
