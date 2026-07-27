using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Store.Commands;
using ClaudyGod.Application.Features.Store.DTOs;
using ClaudyGod.Domain.Entities;
using ClaudyGod.Domain.Exceptions;
using ClaudyGod.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ClaudyGod.Application.Tests.Features.Store;

public class CreateOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_ClientPriceDoesNotMatchCatalog_RejectsOrder()
    {
        await using var db = CreateContext();
        var product = Product.Create("Premium Shirt", "Official shirt", 50m,
            "https://example.com/shirt.jpg", "apparel", quantity: 5);
        db.Products.Add(product);
        await db.SaveChangesAsync();
        var handler = new CreateOrderCommandHandler(db, Substitute.For<IPaystackService>());
        var request = BuildRequest(product.Id, clientPrice: 1m, subtotal: 1m, total: 6m);

        var act = () => handler.Handle(new CreateOrderCommand(request), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*current product catalog*");
        (await db.Orders.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_ValidCatalogTotals_PersistsCanonicalPriceAndReservesStock()
    {
        await using var db = CreateContext();
        var product = Product.Create("Premium Shirt", "Official shirt", 50m,
            "https://example.com/shirt.jpg", "apparel", quantity: 5);
        db.Products.Add(product);
        await db.SaveChangesAsync();
        var handler = new CreateOrderCommandHandler(db, Substitute.For<IPaystackService>());
        var request = BuildRequest(product.Id, clientPrice: 50m, subtotal: 100m, total: 105m);

        var id = await handler.Handle(new CreateOrderCommand(request), CancellationToken.None);

        var order = await db.Orders.SingleAsync(x => x.Id == id);
        order.Subtotal.Should().Be(100m);
        order.Total.Should().Be(105m);
        order.ItemsJson.Should().Contain("50");
        product.Quantity.Should().Be(3);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static CreateOrderRequest BuildRequest(Guid productId, decimal clientPrice,
        decimal subtotal, decimal total) => new(
        [new LineItem(productId.ToString(), "Client supplied name", clientPrice, 2,
            "client-image", "client-category", "client-description")],
        new ShippingInfo("Test Buyer", "buyer@example.com", "+1234567890",
            "1 Test Street", "Lagos", "Lagos", "Nigeria", "100001"),
        "standard",
        "bank_transfer",
        subtotal,
        5m,
        total,
        "USD",
        null);
}
