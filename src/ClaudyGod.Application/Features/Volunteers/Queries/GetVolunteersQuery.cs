using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Volunteers.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Volunteers.Queries;

public record GetVolunteersQuery(int Page = 1, int PageSize = 20, bool? IsApproved = null)
    : IRequest<PaginatedResult<VolunteerDto>>;

public class GetVolunteersQueryHandler : IRequestHandler<GetVolunteersQuery, PaginatedResult<VolunteerDto>>
{
    private readonly IApplicationDbContext _db;

    public GetVolunteersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedResult<VolunteerDto>> Handle(GetVolunteersQuery request, CancellationToken ct)
    {
        var query = _db.Volunteers.AsNoTracking();

        if (request.IsApproved.HasValue)
            query = query.Where(v => v.IsApproved == request.IsApproved.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(v => v.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(v => new VolunteerDto(v.Id, v.FirstName, v.LastName, v.Email,
                v.Role.ToString(), v.Reason, v.IsApproved, v.ApprovedAt, v.CreatedAt))
            .ToListAsync(ct);

        return PaginatedResult<VolunteerDto>.Create(items, total, request.Page, request.PageSize);
    }
}
