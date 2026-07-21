using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Domain.Entities;
using ClaudyGod.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Payments.Commands;

public record RecordPaystackPaymentCommand(
    string DonorName,
    string DonorEmail,
    decimal Amount,
    string Currency,
    string Reference,
    string? Message) : IRequest<Guid>;

public class RecordPaystackPaymentCommandValidator : AbstractValidator<RecordPaystackPaymentCommand>
{
    public RecordPaystackPaymentCommandValidator()
    {
        RuleFor(x => x.DonorName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DonorEmail).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Reference).NotEmpty().MaximumLength(100);
    }
}

public class RecordPaystackPaymentCommandHandler : IRequestHandler<RecordPaystackPaymentCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaystackService _paystack;

    public RecordPaystackPaymentCommandHandler(IApplicationDbContext context, IPaystackService paystack)
    {
        _context = context;
        _paystack = paystack;
    }

    public async Task<Guid> Handle(RecordPaystackPaymentCommand request, CancellationToken ct)
    {
        // Replay guard — fail fast before calling out to Paystack. The unique index on
        // Reference (PaystackPaymentConfiguration) is the last-resort backstop against races.
        var alreadyRecorded = await _context.PaystackPayments
            .AnyAsync(p => p.Reference == request.Reference, ct);
        if (alreadyRecorded)
            throw new DuplicateResourceException("This payment reference has already been recorded.");

        // Throws ServiceUnavailableException (-> 503) if Paystack:SecretKey isn't configured yet.
        var verification = await _paystack.VerifyTransactionAsync(request.Reference, ct);

        if (!verification.Success)
            throw new DomainException("This payment could not be verified with Paystack and was not recorded.");

        // Paystack reports amounts in minor units (e.g. kobo for NGN, cents for USD).
        var expectedMinorUnits = (long)Math.Round(request.Amount * 100, MidpointRounding.AwayFromZero);
        if (verification.AmountInMinorUnits != expectedMinorUnits ||
            !string.Equals(verification.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("The verified payment amount or currency does not match what was submitted.");
        }

        var payment = PaystackPayment.Create(
            request.DonorName,
            request.DonorEmail,
            request.Amount,
            request.Currency,
            request.Reference,
            request.Message);
        payment.MarkAsVerified();

        _context.PaystackPayments.Add(payment);

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Concurrent request recorded the same reference first — the unique index caught it.
            throw new DuplicateResourceException("This payment reference has already been recorded.");
        }

        return payment.Id;
    }
}
