using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Albums.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Albums.Commands;

public record UpdateAlbumCommand(Guid AlbumId, CreateAlbumRequest Request) : IRequest;

public class UpdateAlbumCommandValidator : AbstractValidator<UpdateAlbumCommand>
{
    public UpdateAlbumCommandValidator()
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

public class UpdateAlbumCommandHandler : IRequestHandler<UpdateAlbumCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdateAlbumCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UpdateAlbumCommand request, CancellationToken ct)
    {
        var album = await _db.Albums.FirstOrDefaultAsync(a => a.Id == request.AlbumId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("Album not found.");

        var r = request.Request;
        album.Update(r.Title, r.ImageUrl, r.SpotifyUrl, r.AppleUrl,
            r.YoutubeUrl, r.DeezerUrl, r.AmazonUrl, r.SortOrder, r.ReleasedAt);

        await _db.SaveChangesAsync(ct);
    }
}
