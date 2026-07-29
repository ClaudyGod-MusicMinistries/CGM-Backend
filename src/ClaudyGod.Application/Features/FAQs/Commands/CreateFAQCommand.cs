using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.FAQs.DTOs;
using ClaudyGod.Domain.Entities;
using FluentValidation;
using MediatR;

namespace ClaudyGod.Application.Features.FAQs.Commands;

public record CreateFAQCommand(CreateFAQRequest Request) : IRequest<Guid>;

public class CreateFAQCommandValidator : AbstractValidator<CreateFAQCommand>
{
    public CreateFAQCommandValidator()
    {
        RuleFor(x => x.Request.Question).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Request.Answer).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Request.Category).NotEmpty().MaximumLength(100);
    }
}

public class CreateFAQCommandHandler : IRequestHandler<CreateFAQCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CreateFAQCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateFAQCommand request, CancellationToken ct)
    {
        var r = request.Request;

        var faq = FAQ.Create(r.Question, r.Answer, r.Category, r.Order);

        _db.FAQs.Add(faq);
        await _db.SaveChangesAsync(ct);

        return faq.Id;
    }
}
