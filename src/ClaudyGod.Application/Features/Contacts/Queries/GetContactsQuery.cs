using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Contacts.Queries;

public record ContactMessageDto(Guid Id, string Name, string Email, string Message,
    bool IsRead, DateTime? ReadAt, DateTime CreatedAt);

public record GetContactsQuery(int Page = 1, int PageSize = 20, bool? IsRead = null)
    : IRequest<PaginatedResult<ContactMessageDto>>;

public class GetContactsQueryHandler : IRequestHandler<GetContactsQuery, PaginatedResult<ContactMessageDto>>
{
    private readonly IApplicationDbContext _db;

    public GetContactsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedResult<ContactMessageDto>> Handle(GetContactsQuery request, CancellationToken ct)
    {
        var query = _db.ContactMessages.AsNoTracking();

        if (request.IsRead.HasValue)
            query = query.Where(c => c.IsRead == request.IsRead.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new ContactMessageDto(c.Id, c.Name, c.Email, c.Message,
                c.IsRead, c.ReadAt, c.CreatedAt))
            .ToListAsync(ct);

        return PaginatedResult<ContactMessageDto>.Create(items, total, request.Page, request.PageSize);
    }
}
