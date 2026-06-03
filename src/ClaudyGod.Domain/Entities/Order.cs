using ClaudyGod.Domain.Enums;

namespace ClaudyGod.Domain.Entities;

public class Order : AuditableEntity
{
    public string OrderId { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string ShippingAddress { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string ItemsJson { get; private set; } = "[]"; // JSON array of items
    public string ShippingMethod { get; private set; } = string.Empty; // "standard" or "express"
    public string PaymentMethod { get; private set; } = string.Empty; // "paystack", "card", "bank_transfer", "paypal"
    public decimal Subtotal { get; private set; }
    public decimal ShippingCost { get; private set; }
    public decimal Total { get; private set; }
    public string Currency { get; private set; } = "USD";
    public string? PaystackReference { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public string? AdminNotes { get; private set; }

    protected Order() { }

    public static Order Create(
        string orderId, string fullName, string email, string phone,
        string shippingAddress, string city, string state, string country,
        string postalCode, string itemsJson, string shippingMethod,
        string paymentMethod, decimal subtotal, decimal shippingCost,
        decimal total, string currency, string? paystackRef = null)
    {
        return new Order
        {
            OrderId = orderId,
            FullName = fullName.Trim(),
            Email = email.ToLowerInvariant().Trim(),
            Phone = phone.Trim(),
            ShippingAddress = shippingAddress.Trim(),
            City = city.Trim(),
            State = state.Trim(),
            Country = country.Trim(),
            PostalCode = postalCode.Trim(),
            ItemsJson = itemsJson,
            ShippingMethod = shippingMethod,
            PaymentMethod = paymentMethod,
            Subtotal = subtotal,
            ShippingCost = shippingCost,
            Total = total,
            Currency = currency,
            PaystackReference = paystackRef,
            Status = OrderStatus.Pending
        };
    }

    public void UpdateStatus(OrderStatus status, string? notes = null)
    {
        Status = status;
        if (notes is not null) AdminNotes = notes;
    }

    public void SetPaystackReference(string reference) => PaystackReference = reference;
}
