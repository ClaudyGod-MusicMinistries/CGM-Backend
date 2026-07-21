namespace ClaudyGod.Application.Common.Interfaces;

public interface IPaystackService
{
    bool IsConfigured { get; }

    /// <summary>Verify a transaction reference against Paystack's API. Throws ServiceUnavailableException if not configured or unreachable.</summary>
    Task<PaystackVerificationResult> VerifyTransactionAsync(string reference, CancellationToken ct = default);
}

public record PaystackVerificationResult(
    bool Success,
    string Status,
    long AmountInMinorUnits,
    string Currency,
    string Reference);
