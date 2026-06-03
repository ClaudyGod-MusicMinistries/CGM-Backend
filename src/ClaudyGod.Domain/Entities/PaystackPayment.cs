namespace ClaudyGod.Domain.Entities;

public class PaystackPayment : AuditableEntity
{
    public string DonorName { get; private set; } = string.Empty;
    public string DonorEmail { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "NGN";
    public string Reference { get; private set; } = string.Empty;
    public bool IsVerified { get; private set; } = false;
    public DateTime? VerifiedAt { get; private set; }
    public string? Message { get; private set; }

    protected PaystackPayment() { }

    public static PaystackPayment Create(
        string donorName, string donorEmail, decimal amount,
        string currency, string reference, string? message = null)
    {
        return new PaystackPayment
        {
            DonorName = donorName.Trim(),
            DonorEmail = donorEmail.ToLowerInvariant().Trim(),
            Amount = amount,
            Currency = currency,
            Reference = reference,
            Message = message,
            IsVerified = false
        };
    }

    public void MarkAsVerified()
    {
        IsVerified = true;
        VerifiedAt = DateTime.UtcNow;
    }
}
