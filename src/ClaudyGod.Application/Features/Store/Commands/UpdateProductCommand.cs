using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Store.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Store.Commands;

public record UpdateProductCommand(Guid ProductId, CreateProductRequest Request) : IRequest;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Request.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Request.Description).NotEmpty();
        RuleFor(x => x.Request.Price).GreaterThan(0);
        RuleFor(x => x.Request.Image).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Request.Category).NotEmpty().MaximumLength(100);
    }
}

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdateProductCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("Product not found.");

        var r = request.Request;
        product.Update(r.Title, r.Description, r.Price, r.Image, r.Category,
            r.InStock, r.Quantity, r.Rating, r.SortOrder);

        await _db.SaveChangesAsync(ct);
    }
}
