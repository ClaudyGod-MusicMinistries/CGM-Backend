using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Store.DTOs;
using ClaudyGod.Domain.Entities;
using FluentValidation;
using MediatR;

namespace ClaudyGod.Application.Features.Store.Commands;

public record CreateProductCommand(CreateProductRequest Request) : IRequest<Guid>;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Request.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Request.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Request.Price).GreaterThan(0);
        RuleFor(x => x.Request.Image).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Request.Category).NotEmpty().MaximumLength(100);
    }
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CreateProductCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var r = request.Request;

        var product = Product.Create(r.Title, r.Description, r.Price, r.Image,
            r.Category, r.InStock, r.Quantity, r.Rating, r.SortOrder);

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);

        return product.Id;
    }
}
