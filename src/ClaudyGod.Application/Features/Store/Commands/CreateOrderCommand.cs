using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Store.DTOs;
using ClaudyGod.Domain.Entities;
using MediatR;
using System.Text.Json;

namespace ClaudyGod.Application.Features.Store.Commands;

public record CreateOrderCommand(CreateOrderRequest Request) : IRequest<Guid>;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateOrderCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var req = request.Request;
        var itemsJson = JsonSerializer.Serialize(req.Items);

        var order = Order.Create(
            orderId: GenerateOrderId(),
            fullName: req.Shipping.FullName,
            email: req.Shipping.Email,
            phone: req.Shipping.Phone,
            shippingAddress: req.Shipping.Address,
            city: req.Shipping.City,
            state: req.Shipping.State,
            country: req.Shipping.Country,
            postalCode: req.Shipping.PostalCode ?? string.Empty,
            itemsJson: itemsJson,
            shippingMethod: req.ShippingMethod,
            paymentMethod: req.PaymentMethod,
            subtotal: req.Subtotal,
            shippingCost: req.ShippingCost,
            total: req.Total,
            currency: req.Currency,
            paystackRef: req.PaystackRef);

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(ct);

        return order.Id;
    }

    private static string GenerateOrderId() => $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(10000, 99999)}";
}
