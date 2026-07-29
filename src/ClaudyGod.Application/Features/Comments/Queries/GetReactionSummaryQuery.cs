using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Comments.Commands;
using ClaudyGod.Application.Features.Comments.DTOs;
using MediatR;

namespace ClaudyGod.Application.Features.Comments.Queries;

public record GetReactionSummaryQuery(Guid? BlogPostId, Guid? CommentId, string? VisitorToken)
    : IRequest<ReactionSummaryDto>;

public class GetReactionSummaryQueryHandler : IRequestHandler<GetReactionSummaryQuery, ReactionSummaryDto>
{
    private readonly IApplicationDbContext _db;

    public GetReactionSummaryQueryHandler(IApplicationDbContext db) => _db = db;

    public Task<ReactionSummaryDto> Handle(GetReactionSummaryQuery request, CancellationToken ct) =>
        SetReactionCommandHandler.BuildSummary(
            _db, request.BlogPostId, request.CommentId, request.VisitorToken ?? string.Empty, ct);
}
