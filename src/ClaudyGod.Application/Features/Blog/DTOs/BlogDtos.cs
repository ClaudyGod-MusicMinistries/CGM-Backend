namespace ClaudyGod.Application.Features.Blog.DTOs;

public record CreateBlogPostRequest(
    string Title,
    string Slug,
    string Content,
    string? Excerpt,
    string? AuthorName,
    Guid? CategoryId,
    List<Guid>? TagIds,
    bool Publish = false);

public record UpdateBlogPostRequest(
    string Title,
    string Slug,
    string Content,
    string? Excerpt,
    string? AuthorName,
    Guid? CategoryId,
    List<Guid>? TagIds);

public record BlogPostDto(
    Guid Id,
    string Title,
    string Slug,
    string? Excerpt,
    string? FeaturedImagePath,
    string Status,
    DateTime? PublishedAt,
    string? AuthorName,
    string? CategoryName,
    List<string> Tags,
    int ViewCount,
    bool IsFeatured,
    DateTime CreatedAt);

public record BlogPostDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string Content,
    string? Excerpt,
    string? FeaturedImagePath,
    string Status,
    DateTime? PublishedAt,
    string? AuthorName,
    Guid? CategoryId,
    string? CategoryName,
    List<string> Tags,
    int ViewCount,
    bool IsFeatured,
    DateTime CreatedAt);

public record BlogCategoryDto(Guid Id, string Name);
public record BlogTagDto(Guid Id, string Name);
