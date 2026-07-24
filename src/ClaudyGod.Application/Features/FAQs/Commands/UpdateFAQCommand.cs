using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.FAQs.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.FAQs.Commands;

public record UpdateFAQCommand(Guid FAQId, CreateFAQRequest Request) : IRequest;

public class UpdateFAQCommandValidator : AbstractValidator<UpdateFAQCommand>
{
    public UpdateFAQCommandValidator()
    {
        RuleFor(x => x.Request.Question).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Request.Answer).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Request.Category).NotEmpty().MaximumLength(100);
    }
}

public class UpdateFAQCommandHandler : IRequestHandler<UpdateFAQCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdateFAQCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UpdateFAQCommand request, CancellationToken ct)
    {
        var faq = await _db.FAQs.FirstOrDefaultAsync(f => f.Id == request.FAQId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("FAQ not found.");

        var r = request.Request;
        faq.Update(r.Question, r.Answer, r.Category, r.Order);

        await _db.SaveChangesAsync(ct);
    }
}
