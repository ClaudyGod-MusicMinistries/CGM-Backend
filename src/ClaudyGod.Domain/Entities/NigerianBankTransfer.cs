using ClaudyGod.Domain.Enums;

namespace ClaudyGod.Domain.Entities;

public class NigerianBankTransfer : AuditableEntity
{
    public string Reference { get; private set; } = string.Empty;
    public string SenderName { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "NGN";
    public string SlipFilePath { get; private set; } = string.Empty;
    public string SlipContentType { get; private set; } = string.Empty;
    public TransferStatus Status { get; private set; } = TransferStatus.Pending;
    public DateTime? ValidatedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    protected NigerianBankTransfer() { }

    public static NigerianBankTransfer Create(string reference, string senderName,
        decimal amount, string slipFilePath, string slipContentType, string currency = "NGN") =>
        new()
        {
            Reference = reference.Trim(),
            SenderName = senderName.Trim(),
            Amount = amount,
            Currency = currency,
            SlipFilePath = slipFilePath,
            SlipContentType = slipContentType
        };

    public void Validate()
    {
        Status = TransferStatus.Validated;
        ValidatedAt = DateTime.UtcNow;
    }

    public void Reject(string reason)
    {
        Status = TransferStatus.Rejected;
        RejectionReason = reason;
    }
}
