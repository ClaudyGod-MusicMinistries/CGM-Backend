using ClaudyGod.Domain.Enums;

namespace ClaudyGod.Domain.Entities;

public class Payment : AuditableEntity
{
    public string OrderReference { get; private set; } = string.Empty;
    public string ConfirmationId { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string? UserEmail { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = null!;

    protected Payment() { }

    public static Payment Create(Guid orderId, string orderReference, string confirmationId,
        decimal amount, PaymentMethod paymentMethod, string? userEmail = null) =>
        new()
        {
            OrderId = orderId,
            OrderReference = orderReference,
            ConfirmationId = confirmationId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            UserEmail = userEmail
        };

    public void Confirm() => Status = PaymentStatus.Confirmed;
    public void Fail() => Status = PaymentStatus.Failed;
}
