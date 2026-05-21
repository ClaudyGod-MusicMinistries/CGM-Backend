using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Payments.Commands;

public record ValidateNigerianTransferCommand(
    string Reference,
    string SenderName,
    decimal Amount,
    string Currency,
    IFormFile SlipFile) : IRequest<Guid>;

public class ValidateNigerianTransferCommandValidator : AbstractValidator<ValidateNigerianTransferCommand>
{
    public ValidateNigerianTransferCommandValidator()
    {
        RuleFor(x => x.Reference).NotEmpty().Length(8, 20);
        RuleFor(x => x.SenderName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).Must(c => c is "NGN" or "USD").WithMessage("Currency must be NGN or USD.");
        RuleFor(x => x.SlipFile).NotNull().WithMessage("Payment slip is required.");
    }
}

public class ValidateNigerianTransferCommandHandler : IRequestHandler<ValidateNigerianTransferCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorageService _storage;

    public ValidateNigerianTransferCommandHandler(IApplicationDbContext db, IFileStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<Guid> Handle(ValidateNigerianTransferCommand request, CancellationToken ct)
    {
        var exists = await _db.NigerianBankTransfers
            .AnyAsync(n => n.Reference == request.Reference, ct);

        if (exists) throw new Domain.Exceptions.DuplicateResourceException("Transfer reference already recorded.");

        var fileResult = await _storage.SaveAsync(request.SlipFile, "transfers/ngn",
            [".pdf", ".jpg", ".jpeg", ".png"], 10 * 1024 * 1024, ct);

        var transfer = NigerianBankTransfer.Create(request.Reference, request.SenderName,
            request.Amount, fileResult.RelativePath, fileResult.ContentType, request.Currency);

        _db.NigerianBankTransfers.Add(transfer);
        await _db.SaveChangesAsync(ct);

        return transfer.Id;
    }
}
