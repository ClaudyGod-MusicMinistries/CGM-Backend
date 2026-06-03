using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Domain.Entities;
using MediatR;

namespace ClaudyGod.Application.Features.Payments.Commands;

public record RecordPaystackPaymentCommand(
    string DonorName,
    string DonorEmail,
    decimal Amount,
    string Currency,
    string Reference,
    string? Message) : IRequest<Guid>;

public class RecordPaystackPaymentCommandHandler : IRequestHandler<RecordPaystackPaymentCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public RecordPaystackPaymentCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Guid> Handle(RecordPaystackPaymentCommand request, CancellationToken ct)
    {
        var payment = PaystackPayment.Create(
            request.DonorName,
            request.DonorEmail,
            request.Amount,
            request.Currency,
            request.Reference,
            request.Message);

        _context.PaystackPayments.Add(payment);
        await _context.SaveChangesAsync(ct);

        return payment.Id;
    }
}
