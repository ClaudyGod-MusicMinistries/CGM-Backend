using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Store.DTOs;
using ClaudyGod.Domain.Entities;
using ClaudyGod.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ClaudyGod.Application.Features.Store.Commands;

public record CreateOrderCommand(CreateOrderRequest Request) : IRequest<Guid>;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    private static readonly string[] SupportedPaymentMethods = ["paystack", "bank_transfer"];

    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.Request.Items).NotEmpty().Must(items => items.Count <= 50);
        RuleForEach(x => x.Request.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Id).Must(id => Guid.TryParse(id, out _)).WithMessage("A valid product id is required.");
            item.RuleFor(x => x.Quantity).InclusiveBetween(1, 20);
        });
        RuleFor(x => x.Request.Shipping.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Shipping.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Request.Shipping.Phone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Request.Shipping.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Request.Shipping.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.Shipping.State).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.Shipping.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.ShippingMethod).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Request.PaymentMethod)
            .Must(value => SupportedPaymentMethods.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Supported payment methods are paystack and bank_transfer.");
        RuleFor(x => x.Request.Subtotal).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.ShippingCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.Total).GreaterThan(0);
        RuleFor(x => x.Request.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Request.PaystackRef)
            .NotEmpty()
            .When(x => x.Request.PaymentMethod.Equals("paystack", StringComparison.OrdinalIgnoreCase));
    }
}

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaystackService _paystack;

    public CreateOrderCommandHandler(IApplicationDbContext context, IPaystackService paystack)
    {
        _context = context;
        _paystack = paystack;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var req = request.Request;
        var requestedItems = req.Items
            .GroupBy(item => Guid.Parse(item.Id))
            .Select(group => new { ProductId = group.Key, Quantity = group.Sum(item => item.Quantity) })
            .ToList();

        if (requestedItems.Any(item => item.Quantity > 20))
            throw new ClaudyGod.Domain.Exceptions.ValidationException("A product quantity cannot exceed 20 per order.");

        var productIds = requestedItems.Select(item => item.ProductId).ToList();
        var products = await _context.Products
            .Where(product => productIds.Contains(product.Id) && product.IsPublished)
            .ToDictionaryAsync(product => product.Id, ct);

        if (products.Count != productIds.Count)
            throw new NotFoundException("One or more products are unavailable.");

        foreach (var item in requestedItems)
            products[item.ProductId].ReserveStock(item.Quantity);

        var canonicalItems = requestedItems.Select(item =>
        {
            var product = products[item.ProductId];
            return new LineItem(product.Id.ToString(), product.Title, product.Price, item.Quantity,
                product.ImageUrl, product.Category, product.Description);
        }).ToList();

        var canonicalSubtotal = canonicalItems.Sum(item => item.Price * item.Quantity);
        var expectedTotal = canonicalSubtotal + req.ShippingCost;
        if (req.Subtotal != canonicalSubtotal || req.Total != expectedTotal)
            throw new DomainException("Order totals do not match the current product catalog.");

        if (req.PaymentMethod.Equals("paystack", StringComparison.OrdinalIgnoreCase))
        {
            if (await _context.Orders.AnyAsync(order => order.PaystackReference == req.PaystackRef, ct))
                throw new DuplicateResourceException("This Paystack payment has already been used for an order.");

            var verification = await _paystack.VerifyTransactionAsync(req.PaystackRef!, ct);
            var expectedMinorUnits = decimal.ToInt64(decimal.Round(req.Total * 100m, 0, MidpointRounding.AwayFromZero));
            if (!verification.Success || verification.Reference != req.PaystackRef ||
                verification.AmountInMinorUnits != expectedMinorUnits ||
                !verification.Currency.Equals(req.Currency, StringComparison.OrdinalIgnoreCase))
                throw new DomainException("The Paystack payment does not match this order.");
        }

        var itemsJson = JsonSerializer.Serialize(canonicalItems);

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
            subtotal: canonicalSubtotal,
            shippingCost: req.ShippingCost,
            total: expectedTotal,
            currency: req.Currency.ToUpperInvariant(),
            paystackRef: req.PaystackRef);

        _context.Orders.Add(order);
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DomainException("Product availability changed while placing the order. Please review your cart and try again.");
        }

        return order.Id;
    }

    private static string GenerateOrderId() =>
        $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}";
}
