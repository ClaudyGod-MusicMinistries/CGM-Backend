using ClaudyGod.Application.Common.Interfaces;
using MediatR;

namespace ClaudyGod.Application.Features.Payments.Queries;

public record GetPaymentMethodsStatusQuery : IRequest<PaymentMethodsStatusDto>;

public record PaymentMethodsStatusDto(bool Paystack, bool Zelle, bool NgnTransfer);

public class GetPaymentMethodsStatusQueryHandler
    : IRequestHandler<GetPaymentMethodsStatusQuery, PaymentMethodsStatusDto>
{
    private readonly IPaystackService _paystack;

    public GetPaymentMethodsStatusQueryHandler(IPaystackService paystack) => _paystack = paystack;

    public Task<PaymentMethodsStatusDto> Handle(GetPaymentMethodsStatusQuery request, CancellationToken ct) =>
        // Zelle and Nigerian bank transfer are manual/admin-reviewed flows with no external
        // dependency, so they're always available. Paystack depends on a configured secret key.
        Task.FromResult(new PaymentMethodsStatusDto(
            Paystack: _paystack.IsConfigured,
            Zelle: true,
            NgnTransfer: true));
}
