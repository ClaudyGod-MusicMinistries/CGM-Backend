using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Payments.Commands;

public record ValidateZellePaymentCommand(
    string TransactionId,
    decimal Amount,
    string? SenderEmail,
    string? SenderPhone,
    string? Purpose,
    Guid? OrderId) : IRequest<Guid>;

public class ValidateZellePaymentCommandValidator : AbstractValidator<ValidateZellePaymentCommand>
{
    public ValidateZellePaymentCommandValidator()
    {
        RuleFor(x => x.TransactionId).NotEmpty().Length(9, 12)
            .Matches("^[A-Z0-9]+$").WithMessage("Transaction ID must be uppercase alphanumeric.");
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public class ValidateZellePaymentCommandHandler : IRequestHandler<ValidateZellePaymentCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public ValidateZellePaymentCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(ValidateZellePaymentCommand request, CancellationToken ct)
    {
        var existing = await _db.ZellePayments
            .AnyAsync(z => z.TransactionId == request.TransactionId.ToUpperInvariant(), ct);

        if (existing) throw new Domain.Exceptions.DuplicateResourceException("Transaction ID already recorded.");

        var payment = ZellePayment.Create(request.TransactionId, request.Amount,
            request.SenderEmail, request.SenderPhone, "USD", request.Purpose, request.OrderId);

        _db.ZellePayments.Add(payment);
        await _db.SaveChangesAsync(ct);

        return payment.Id;
    }
}
