namespace ClaudyGod.Application.Features.Store.DTOs;

// Property names match the frontend's `StoreProduct` contract (lib/data/types.ts)
// field-for-field under camelCase JSON serialization — `Image` here, not
// `ImageUrl`, so the wire shape needs no adapter changes on the frontend.
public record ProductDto(
    Guid Id,
    string Title,
    string Description,
    decimal Price,
    string Image,
    string Category,
    bool InStock,
    int? Quantity,
    decimal? Rating);

public record CreateProductRequest(
    string Title,
    string Description,
    decimal Price,
    string Image,
    string Category,
    bool InStock = true,
    int? Quantity = null,
    decimal? Rating = null,
    int SortOrder = 0);
