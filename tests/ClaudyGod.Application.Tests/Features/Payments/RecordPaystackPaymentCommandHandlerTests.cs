using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Payments.Commands;
using ClaudyGod.Domain.Exceptions;
using ClaudyGod.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ClaudyGod.Application.Tests.Features.Payments;

public class RecordPaystackPaymentCommandHandlerTests
{
    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static RecordPaystackPaymentCommand ValidCommand(string reference = "ref-123") => new(
        DonorName: "Peter",
        DonorEmail: "peter@claudygod.org",
        Amount: 100m,
        Currency: "NGN",
        Reference: reference,
        Message: null);

    [Fact]
    public async Task Handle_WhenReferenceAlreadyRecorded_ThrowsDuplicateWithoutCallingPaystack()
    {
        await using var db = NewContext();
        var command = ValidCommand("dup-ref");
        db.PaystackPayments.Add(Domain.Entities.PaystackPayment.Create(
            "Existing", "existing@example.com", 100m, "NGN", "dup-ref"));
        await db.SaveChangesAsync();

        var paystack = Substitute.For<IPaystackService>();
        var handler = new RecordPaystackPaymentCommandHandler(db, paystack);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateResourceException>();
        await paystack.DidNotReceive().VerifyTransactionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPaystackNotConfigured_PropagatesServiceUnavailable()
    {
        await using var db = NewContext();
        var paystack = Substitute.For<IPaystackService>();
        paystack.VerifyTransactionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<PaystackVerificationResult>>(_ => throw new ServiceUnavailableException(
                "Paystack", "The payment gateway is still being configured and isn't available yet."));

        var handler = new RecordPaystackPaymentCommandHandler(db, paystack);

        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ServiceUnavailableException>();
        (await db.PaystackPayments.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenVerificationFails_ThrowsDomainExceptionAndPersistsNothing()
    {
        await using var db = NewContext();
        var paystack = Substitute.For<IPaystackService>();
        paystack.VerifyTransactionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PaystackVerificationResult(false, "failed", 10000, "NGN", "ref-123"));

        var handler = new RecordPaystackPaymentCommandHandler(db, paystack);

        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        (await db.PaystackPayments.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenAmountMismatch_ThrowsDomainExceptionAndPersistsNothing()
    {
        await using var db = NewContext();
        var paystack = Substitute.For<IPaystackService>();
        // Command claims 100 NGN (10000 kobo); Paystack says only 50 NGN was actually paid.
        paystack.VerifyTransactionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PaystackVerificationResult(true, "success", 5000, "NGN", "ref-123"));

        var handler = new RecordPaystackPaymentCommandHandler(db, paystack);

        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        (await db.PaystackPayments.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenVerified_PersistsPaymentAsVerified()
    {
        await using var db = NewContext();
        var paystack = Substitute.For<IPaystackService>();
        paystack.VerifyTransactionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PaystackVerificationResult(true, "success", 10000, "NGN", "ref-123"));

        var handler = new RecordPaystackPaymentCommandHandler(db, paystack);

        var id = await handler.Handle(ValidCommand(), CancellationToken.None);

        var saved = await db.PaystackPayments.SingleAsync(p => p.Id == id);
        saved.IsVerified.Should().BeTrue();
        saved.VerifiedAt.Should().NotBeNull();
    }
}
