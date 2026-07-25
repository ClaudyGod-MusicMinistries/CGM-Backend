using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Storage.DTOs;
using FluentValidation;
using MediatR;

namespace ClaudyGod.Application.Features.Storage.Commands;

public record RequestUploadCommand(RequestUploadRequest Request) : IRequest<PresignedUploadResult>;

public class RequestUploadCommandValidator : AbstractValidator<RequestUploadCommand>
{
    public RequestUploadCommandValidator()
    {
        RuleFor(x => x.Request.FileName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Request.MimeType).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Request.FileSizeBytes).GreaterThan(0);
    }
}

public class RequestUploadCommandHandler : IRequestHandler<RequestUploadCommand, PresignedUploadResult>
{
    private readonly IWebsiteStorageService _storage;
    private readonly ICurrentUserService _currentUser;

    public RequestUploadCommandHandler(IWebsiteStorageService storage, ICurrentUserService currentUser)
    {
        _storage = storage;
        _currentUser = currentUser;
    }

    public Task<PresignedUploadResult> Handle(RequestUploadCommand request, CancellationToken ct)
    {
        var r = request.Request;
        var requestedBy = _currentUser.UserId ?? "anonymous";

        return _storage.CreatePresignedUploadAsync(r.Kind, r.FileName, r.MimeType, r.FileSizeBytes, requestedBy, ct);
    }
}
