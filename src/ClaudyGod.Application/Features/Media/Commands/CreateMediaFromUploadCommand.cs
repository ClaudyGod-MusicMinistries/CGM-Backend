using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Media.DTOs;
using ClaudyGod.Domain.Entities;
using ClaudyGod.Domain.Enums;
using ClaudyGod.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = ClaudyGod.Domain.Exceptions.ValidationException;

namespace ClaudyGod.Application.Features.Media.Commands;

public record CreateMediaFromUploadCommand(CreateMediaFromUploadRequest Request) : IRequest<Guid>;

public class CreateMediaFromUploadCommandValidator : AbstractValidator<CreateMediaFromUploadCommand>
{
    public CreateMediaFromUploadCommandValidator()
    {
        RuleFor(x => x.Request.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Request.SessionId).NotEmpty();
    }
}

public class CreateMediaFromUploadCommandHandler : IRequestHandler<CreateMediaFromUploadCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    private static readonly Dictionary<MediaType, UploadAssetKind> KindByMediaType = new()
    {
        [MediaType.Photo] = UploadAssetKind.Thumbnail,
        [MediaType.Music] = UploadAssetKind.Audio,
        [MediaType.SermonAudio] = UploadAssetKind.Audio,
        [MediaType.SermonVideo] = UploadAssetKind.Video,
        [MediaType.Video] = UploadAssetKind.Video,
        [MediaType.Other] = UploadAssetKind.Document,
    };

    public CreateMediaFromUploadCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateMediaFromUploadCommand request, CancellationToken ct)
    {
        var r = request.Request;

        var session = await _db.UploadSessions.FirstOrDefaultAsync(s => s.Id == r.SessionId, ct)
            ?? throw new NotFoundException("Upload session", r.SessionId);

        if (session.Status != UploadSessionStatus.Uploaded)
            throw new ValidationException(
                "This upload has not been confirmed yet — complete the upload and confirm step before creating the media item.");

        var expectedKind = KindByMediaType[r.Type];
        if (session.AssetKind != expectedKind)
            throw new ValidationException(
                $"The uploaded file ({session.AssetKind}) does not match the declared media type '{r.Type}' (expects {expectedKind}).");

        var media = MediaItem.Create(r.Title, r.Type, session.StorageKey,
            session.OriginalFileName, session.MimeType, session.ActualFileSizeBytes ?? session.DeclaredFileSizeBytes ?? 0,
            r.Description, r.ArtistName, r.AlbumName);

        _db.MediaItems.Add(media);
        await _db.SaveChangesAsync(ct);

        return media.Id;
    }
}
