using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Media.DTOs;
using ClaudyGod.Domain.Entities;
using FluentValidation;
using MediatR;

namespace ClaudyGod.Application.Features.Media.Commands;

/// <summary>
/// Registers externally-hosted media (a YouTube link, etc.) — the counterpart
/// to <see cref="UploadMediaCommand"/> for content that isn't a real file
/// upload, since every current video is YouTube-hosted, not self-hosted.
/// </summary>
public record CreateMediaLinkCommand(CreateMediaLinkRequest Request) : IRequest<Guid>;

public class CreateMediaLinkCommandValidator : AbstractValidator<CreateMediaLinkCommand>
{
    public CreateMediaLinkCommandValidator()
    {
        RuleFor(x => x.Request.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Request.ExternalUrl).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Request.ThumbnailUrl).MaximumLength(1000);
    }
}

public class CreateMediaLinkCommandHandler : IRequestHandler<CreateMediaLinkCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CreateMediaLinkCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateMediaLinkCommand request, CancellationToken ct)
    {
        var r = request.Request;

        var media = MediaItem.CreateLink(r.Title, r.Type, r.ExternalUrl, r.ThumbnailUrl, r.Description);

        _db.MediaItems.Add(media);
        await _db.SaveChangesAsync(ct);

        return media.Id;
    }
}
