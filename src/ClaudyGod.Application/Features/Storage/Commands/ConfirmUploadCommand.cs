using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Common.Models;
using FluentValidation;
using MediatR;

namespace ClaudyGod.Application.Features.Storage.Commands;

public record ConfirmUploadCommand(Guid SessionId) : IRequest<ConfirmedUploadResult>;

public class ConfirmUploadCommandValidator : AbstractValidator<ConfirmUploadCommand>
{
    public ConfirmUploadCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
    }
}

public class ConfirmUploadCommandHandler : IRequestHandler<ConfirmUploadCommand, ConfirmedUploadResult>
{
    private readonly IWebsiteStorageService _storage;
    private readonly ICurrentUserService _currentUser;

    public ConfirmUploadCommandHandler(IWebsiteStorageService storage, ICurrentUserService currentUser)
    {
        _storage = storage;
        _currentUser = currentUser;
    }

    public Task<ConfirmedUploadResult> Handle(ConfirmUploadCommand request, CancellationToken ct)
    {
        var requestedBy = _currentUser.UserId ?? "anonymous";
        return _storage.ConfirmUploadAsync(request.SessionId, requestedBy, ct);
    }
}
