using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Albums.DTOs;
using ClaudyGod.Domain.Entities;
using FluentValidation;
using MediatR;

namespace ClaudyGod.Application.Features.Albums.Commands;

public record CreateAlbumCommand(CreateAlbumRequest Request) : IRequest<Guid>;

public class CreateAlbumCommandValidator : AbstractValidator<CreateAlbumCommand>
{
    public CreateAlbumCommandValidator()
    {
        RuleFor(x => x.Request.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Request.ImageUrl).MaximumLength(500);
        RuleFor(x => x.Request.SpotifyUrl).MaximumLength(500);
        RuleFor(x => x.Request.AppleUrl).MaximumLength(500);
        RuleFor(x => x.Request.YoutubeUrl).MaximumLength(500);
        RuleFor(x => x.Request.DeezerUrl).MaximumLength(500);
        RuleFor(x => x.Request.AmazonUrl).MaximumLength(500);
    }
}

public class CreateAlbumCommandHandler : IRequestHandler<CreateAlbumCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CreateAlbumCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateAlbumCommand request, CancellationToken ct)
    {
        var r = request.Request;

        var album = Album.Create(r.Title, r.ImageUrl, r.SpotifyUrl, r.AppleUrl,
            r.YoutubeUrl, r.DeezerUrl, r.AmazonUrl, r.SortOrder, r.ReleasedAt);

        _db.Albums.Add(album);
        await _db.SaveChangesAsync(ct);

        return album.Id;
    }
}
