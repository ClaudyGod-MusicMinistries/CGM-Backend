using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Media.DTOs;
using ClaudyGod.Domain.Entities;
using ClaudyGod.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ClaudyGod.Application.Features.Media.Commands;

public record UploadMediaCommand(UploadMediaRequest Request) : IRequest<Guid>;

public class UploadMediaCommandValidator : AbstractValidator<UploadMediaCommand>
{
    public UploadMediaCommandValidator()
    {
        RuleFor(x => x.Request.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Request.File).NotNull().WithMessage("File is required.");
    }
}

public class UploadMediaCommandHandler : IRequestHandler<UploadMediaCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorageService _storage;

    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
    private static readonly string[] AudioExtensions = [".mp3", ".wav", ".m4a", ".ogg", ".flac"];
    private static readonly string[] VideoExtensions = [".mp4", ".mov", ".avi", ".mkv", ".webm"];
    private const long MaxFileSizeBytes = 500 * 1024 * 1024;

    public UploadMediaCommandHandler(IApplicationDbContext db, IFileStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<Guid> Handle(UploadMediaCommand request, CancellationToken ct)
    {
        var r = request.Request;

        var allowed = r.Type switch
        {
            MediaType.Photo => ImageExtensions,
            MediaType.Music => AudioExtensions,
            MediaType.SermonAudio => AudioExtensions,
            MediaType.SermonVideo => VideoExtensions,
            _ => ImageExtensions
        };

        var result = await _storage.SaveAsync(r.File, r.Type.ToString().ToLower(),
            allowed, MaxFileSizeBytes, ct);

        var media = MediaItem.Create(r.Title, r.Type, result.RelativePath,
            result.FileName, result.ContentType, result.SizeBytes,
            r.Description, r.ArtistName, r.AlbumName);

        _db.MediaItems.Add(media);
        await _db.SaveChangesAsync(ct);

        return media.Id;
    }
}
