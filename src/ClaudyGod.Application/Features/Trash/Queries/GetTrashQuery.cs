using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Trash;
using ClaudyGod.Application.Features.Trash.DTOs;
using ClaudyGod.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Trash.Queries;

public record GetTrashQuery(TrashEntityType? EntityType = null, int Page = 1, int PageSize = 20)
    : IRequest<PaginatedResult<TrashItemDto>>;

public class GetTrashQueryHandler : IRequestHandler<GetTrashQuery, PaginatedResult<TrashItemDto>>
{
    private readonly IApplicationDbContext _db;

    public GetTrashQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedResult<TrashItemDto>> Handle(GetTrashQuery request, CancellationToken ct)
    {
        await TrashPurge.PurgeExpiredAsync(_db, ct);

        var types = request.EntityType.HasValue
            ? new[] { request.EntityType.Value }
            : Enum.GetValues<TrashEntityType>();

        var all = new List<TrashItemDto>();
        foreach (var type in types)
            all.AddRange(await LoadAsync(_db, type, ct));

        var ordered = all.OrderByDescending(x => x.DeletedAt).ToList();
        var pageItems = ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return PaginatedResult<TrashItemDto>.Create(pageItems, ordered.Count, request.Page, request.PageSize);
    }

    private static async Task<List<TrashItemDto>> LoadAsync(
        IApplicationDbContext db, TrashEntityType type, CancellationToken ct)
    {
        switch (type)
        {
            case TrashEntityType.Album:
                return (await db.Albums.IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(ct))
                    .Select(x => new TrashItemDto(x.Id, nameof(TrashEntityType.Album), x.Title,
                        (x.IsPublished ? "Published" : "Draft")
                            + (x.ReleasedAt.HasValue ? $" · {x.ReleasedAt.Value:MMM d, yyyy}" : ""),
                        x.DeletedAt!.Value))
                    .ToList();

            case TrashEntityType.Product:
                return (await db.Products.IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(ct))
                    .Select(x => new TrashItemDto(x.Id, nameof(TrashEntityType.Product), x.Title,
                        $"{x.Category} · {x.Price:C}", x.DeletedAt!.Value))
                    .ToList();

            case TrashEntityType.MediaItem:
                return (await db.MediaItems.IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(ct))
                    .Select(x => new TrashItemDto(x.Id, nameof(TrashEntityType.MediaItem), x.Title,
                        x.Type.ToString() + (string.IsNullOrWhiteSpace(x.ArtistName) ? "" : $" · {x.ArtistName}"),
                        x.DeletedAt!.Value))
                    .ToList();

            case TrashEntityType.FAQ:
                return (await db.FAQs.IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(ct))
                    .Select(x => new TrashItemDto(x.Id, nameof(TrashEntityType.FAQ),
                        TrashPurge.Truncate(x.Question, 80), x.Category, x.DeletedAt!.Value))
                    .ToList();

            case TrashEntityType.Event:
                return (await db.Events.IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(ct))
                    .Select(x => new TrashItemDto(x.Id, nameof(TrashEntityType.Event), x.Title,
                        $"{x.Status} · {x.StartDate:MMM d, yyyy}", x.DeletedAt!.Value))
                    .ToList();

            case TrashEntityType.BlogPost:
                return (await db.BlogPosts.IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(ct))
                    .Select(x => new TrashItemDto(x.Id, nameof(TrashEntityType.BlogPost), x.Title,
                        $"{x.Status} · {x.AuthorName ?? "Unknown author"}", x.DeletedAt!.Value))
                    .ToList();

            case TrashEntityType.Booking:
                return (await db.Bookings.IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(ct))
                    .Select(x => new TrashItemDto(x.Id, nameof(TrashEntityType.Booking),
                        $"{x.FirstName} {x.LastName}", $"{x.EventType} · {x.Status}", x.DeletedAt!.Value))
                    .ToList();

            case TrashEntityType.ContactMessage:
                return (await db.ContactMessages.IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(ct))
                    .Select(x => new TrashItemDto(x.Id, nameof(TrashEntityType.ContactMessage), x.Name,
                        TrashPurge.Truncate(x.Message, 80), x.DeletedAt!.Value))
                    .ToList();

            case TrashEntityType.Volunteer:
                return (await db.Volunteers.IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(ct))
                    .Select(x => new TrashItemDto(x.Id, nameof(TrashEntityType.Volunteer),
                        $"{x.FirstName} {x.LastName}", x.IsApproved ? "Approved" : "Pending",
                        x.DeletedAt!.Value))
                    .ToList();

            case TrashEntityType.PrayerRequest:
                return (await db.PrayerRequests.IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(ct))
                    .Select(x => new TrashItemDto(x.Id, nameof(TrashEntityType.PrayerRequest), x.Name,
                        $"{x.Subject} · {x.Status}", x.DeletedAt!.Value))
                    .ToList();

            case TrashEntityType.TicketReservation:
                return (await db.TicketReservations.IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(ct))
                    .Select(x => new TrashItemDto(x.Id, nameof(TrashEntityType.TicketReservation),
                        $"{x.AttendeeFirstName} {x.AttendeeLastName}",
                        $"{x.ConfirmationCode} · {x.Status}", x.DeletedAt!.Value))
                    .ToList();

            case TrashEntityType.Subscriber:
                return (await db.Subscribers.IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(ct))
                    .Select(x => new TrashItemDto(x.Id, nameof(TrashEntityType.Subscriber), x.Name,
                        x.Email, x.DeletedAt!.Value))
                    .ToList();

            case TrashEntityType.Comment:
                return (await db.Comments.IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(ct))
                    .Select(x => new TrashItemDto(x.Id, nameof(TrashEntityType.Comment), x.AuthorName,
                        TrashPurge.Truncate(x.Content, 80), x.DeletedAt!.Value))
                    .ToList();

            default:
                return [];
        }
    }
}
