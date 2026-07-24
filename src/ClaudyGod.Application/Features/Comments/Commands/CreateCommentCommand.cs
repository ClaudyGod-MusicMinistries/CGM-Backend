using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Comments.DTOs;
using ClaudyGod.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Comments.Commands;

public record CreateCommentCommand(Guid BlogPostId, CreateCommentRequest Request) : IRequest<Guid?>;

public class CreateCommentCommandValidator : AbstractValidator<CreateCommentCommand>
{
    public CreateCommentCommandValidator()
    {
        RuleFor(x => x.Request.AuthorName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.AuthorEmail).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Request.Content).NotEmpty().MaximumLength(4000);
    }
}

public class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, Guid?>
{
    private readonly IApplicationDbContext _db;

    public CreateCommentCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid?> Handle(CreateCommentCommand request, CancellationToken ct)
    {
        var r = request.Request;

        // Honeypot tripped — a bot filled a field real visitors never see.
        // Return null (the controller responds as if it succeeded) without
        // touching the database, so the bot can't tell it was rejected.
        if (!string.IsNullOrWhiteSpace(r.Website)) return null;

        var post = await _db.BlogPosts.FirstOrDefaultAsync(p => p.Id == request.BlogPostId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("Blog post not found.");

        Guid? parentId = null;
        if (r.ParentCommentId.HasValue)
        {
            var parent = await _db.Comments.FirstOrDefaultAsync(
                c => c.Id == r.ParentCommentId.Value && c.BlogPostId == request.BlogPostId, ct)
                ?? throw new Domain.Exceptions.NotFoundException("Parent comment not found.");

            if (parent.ParentCommentId.HasValue)
                throw new Domain.Exceptions.ValidationException(
                    "Replies can only be one level deep — reply to the top-level comment instead.");

            parentId = parent.Id;
        }

        var comment = Comment.Create(request.BlogPostId, r.AuthorName, r.AuthorEmail, r.Content, parentId);

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync(ct);

        return comment.Id;
    }
}
