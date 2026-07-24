using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Common.Utilities;
using ClaudyGod.Domain.Entities;
using FluentValidation;
using MediatR;

namespace ClaudyGod.Application.Features.Blog.Commands;

public record CreateBlogCategoryRequest(string Name, string? Description = null);

public record CreateBlogCategoryCommand(CreateBlogCategoryRequest Request) : IRequest<Guid>;

public class CreateBlogCategoryCommandValidator : AbstractValidator<CreateBlogCategoryCommand>
{
    public CreateBlogCategoryCommandValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(100);
    }
}

public class CreateBlogCategoryCommandHandler : IRequestHandler<CreateBlogCategoryCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CreateBlogCategoryCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateBlogCategoryCommand request, CancellationToken ct)
    {
        var r = request.Request;
        var category = BlogCategory.Create(r.Name, SlugHelper.Generate(r.Name), r.Description);

        _db.BlogCategories.Add(category);
        await _db.SaveChangesAsync(ct);

        return category.Id;
    }
}
