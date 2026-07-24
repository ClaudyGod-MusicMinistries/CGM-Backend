using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Media.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Media.Commands;

public record UpdateMediaLinkCommand(Guid MediaId, CreateMediaLinkRequest Request) : IRequest;

public class UpdateMediaLinkCommandValidator : AbstractValidator<UpdateMediaLinkCommand>
{
    public UpdateMediaLinkCommandValidator()
    {
        RuleFor(x => x.Request.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Request.ExternalUrl).NotEmpty().MaximumLength(1000);
    }
}

public class UpdateMediaLinkCommandHandler : IRequestHandler<UpdateMediaLinkCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdateMediaLinkCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UpdateMediaLinkCommand request, CancellationToken ct)
    {
        var media = await _db.MediaItems.FirstOrDefaultAsync(m => m.Id == request.MediaId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("Media item not found.");

        var r = request.Request;
        media.UpdateLink(r.Title, r.Type, r.ExternalUrl, r.ThumbnailUrl, r.Description);

        await _db.SaveChangesAsync(ct);
    }
}
