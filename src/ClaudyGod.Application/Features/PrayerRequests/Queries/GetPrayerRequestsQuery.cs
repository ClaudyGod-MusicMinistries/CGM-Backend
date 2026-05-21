using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.PrayerRequests.Queries;

public record PrayerRequestDto(Guid Id, string Name, string Email, string Subject,
    string RequestText, bool IsConfidential, string Status,
    string? AdminResponse, DateTime? RespondedAt, DateTime CreatedAt);

public record GetPrayerRequestsQuery(int Page = 1, int PageSize = 20,
    PrayerRequestStatus? Status = null, bool IncludeConfidential = true)
    : IRequest<PaginatedResult<PrayerRequestDto>>;

public class GetPrayerRequestsQueryHandler
    : IRequestHandler<GetPrayerRequestsQuery, PaginatedResult<PrayerRequestDto>>
{
    private readonly IApplicationDbContext _db;

    public GetPrayerRequestsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedResult<PrayerRequestDto>> Handle(
        GetPrayerRequestsQuery request, CancellationToken ct)
    {
        var query = _db.PrayerRequests.AsNoTracking();

        if (request.Status.HasValue)
            query = query.Where(p => p.Status == request.Status.Value);

        if (!request.IncludeConfidential)
            query = query.Where(p => !p.IsConfidential);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new PrayerRequestDto(p.Id, p.Name, p.Email, p.Subject,
                p.RequestText, p.IsConfidential, p.Status.ToString(),
                p.AdminResponse, p.RespondedAt, p.CreatedAt))
            .ToListAsync(ct);

        return PaginatedResult<PrayerRequestDto>.Create(items, total, request.Page, request.PageSize);
    }
}
