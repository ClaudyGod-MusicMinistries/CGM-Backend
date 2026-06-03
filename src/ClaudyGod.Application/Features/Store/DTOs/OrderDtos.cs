using ClaudyGod.Domain.Enums;

namespace ClaudyGod.Application.Features.Store.DTOs;

public record LineItem(
    string Id,
    string Name,
    decimal Price,
    int Quantity,
    string Image,
    string Category,
    string Description);

public record ShippingInfo(
    string FullName,
    string Email,
    string Phone,
    string Address,
    string City,
    string State,
    string Country,
    string? PostalCode);

public record CreateOrderRequest(
    List<LineItem> Items,
    ShippingInfo Shipping,
    string ShippingMethod,
    string PaymentMethod,
    decimal Subtotal,
    decimal ShippingCost,
    decimal Total,
    string Currency,
    string? PaystackRef);

public record OrderDto(
    Guid Id,
    string OrderId,
    string FullName,
    string Email,
    string Phone,
    decimal Total,
    string Currency,
    OrderStatus Status,
    DateTime CreatedAt);
