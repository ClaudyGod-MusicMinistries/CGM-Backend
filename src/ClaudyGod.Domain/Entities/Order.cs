using ClaudyGod.Domain.Enums;
using ClaudyGod.Domain.ValueObjects;

namespace ClaudyGod.Domain.Entities;

public class Order : AuditableEntity
{
    public string OrderReference { get; private set; } = string.Empty;
    private readonly List<OrderItem> _items = [];
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public ShippingInfo ShippingInfo { get; private set; } = null!;
    public PaymentMethod PaymentMethod { get; private set; }
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public string? ZelleConfirmation { get; private set; }
    public string? PayPalTransactionId { get; private set; }
    private readonly List<Payment> _payments = [];
    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();

    protected Order() { }

    public static Order Create(string orderReference, ShippingInfo shippingInfo,
        PaymentMethod paymentMethod, decimal totalAmount) =>
        new()
        {
            OrderReference = orderReference,
            ShippingInfo = shippingInfo,
            PaymentMethod = paymentMethod,
            TotalAmount = totalAmount
        };

    public void AddItem(OrderItem item) => _items.Add(item);

    public void ConfirmPayment(string confirmation)
    {
        if (PaymentMethod == PaymentMethod.Zelle)
            ZelleConfirmation = confirmation;
        else
            PayPalTransactionId = confirmation;
        Status = OrderStatus.Confirmed;
    }

    public void UpdateStatus(OrderStatus status) => Status = status;
}
