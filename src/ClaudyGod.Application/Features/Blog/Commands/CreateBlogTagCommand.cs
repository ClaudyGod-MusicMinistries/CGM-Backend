using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Common.Utilities;
using ClaudyGod.Domain.Entities;
using FluentValidation;
using MediatR;

namespace ClaudyGod.Application.Features.Blog.Commands;

public record CreateBlogTagRequest(string Name);

public record CreateBlogTagCommand(CreateBlogTagRequest Request) : IRequest<Guid>;

public class CreateBlogTagCommandValidator : AbstractValidator<CreateBlogTagCommand>
{
    public CreateBlogTagCommandValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(50);
    }
}

public class CreateBlogTagCommandHandler : IRequestHandler<CreateBlogTagCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CreateBlogTagCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateBlogTagCommand request, CancellationToken ct)
    {
        var r = request.Request;
        var tag = BlogTag.Create(r.Name, SlugHelper.Generate(r.Name));

        _db.BlogTags.Add(tag);
        await _db.SaveChangesAsync(ct);

        return tag.Id;
    }
}
