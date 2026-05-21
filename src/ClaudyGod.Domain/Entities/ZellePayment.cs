using ClaudyGod.Domain.Enums;

namespace ClaudyGod.Domain.Entities;

public class ZellePayment : AuditableEntity
{
    public string? SenderEmail { get; private set; }
    public string? SenderPhone { get; private set; }
    public string TransactionId { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public TransferStatus Status { get; private set; } = TransferStatus.Pending;
    public string? Purpose { get; private set; }
    public Guid? RelatedOrderId { get; private set; }

    protected ZellePayment() { }

    public static ZellePayment Create(string transactionId, decimal amount,
        string? senderEmail = null, string? senderPhone = null,
        string currency = "USD", string? purpose = null, Guid? orderId = null)
    {
        return new ZellePayment
        {
            TransactionId = transactionId.ToUpperInvariant().Trim(),
            Amount = amount,
            SenderEmail = senderEmail?.ToLowerInvariant().Trim(),
            SenderPhone = senderPhone?.Trim(),
            Currency = currency,
            Purpose = purpose,
            RelatedOrderId = orderId
        };
    }

    public void Verify() => Status = TransferStatus.Verified;
    public void Reject() => Status = TransferStatus.Rejected;
}
