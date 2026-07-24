namespace ClaudyGod.Application.Features.Trash.DTOs;

public record TrashItemDto(
    Guid Id,
    string EntityType,
    string Title,
    string Subtitle,
    DateTime DeletedAt);
