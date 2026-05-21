namespace ClaudyGod.Domain.Entities;

public class BlogCategory : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ICollection<BlogPost> Posts { get; private set; } = [];

    protected BlogCategory() { }

    public static BlogCategory Create(string name, string slug, string? description = null) =>
        new() { Name = name.Trim(), Slug = slug.ToLowerInvariant().Trim(), Description = description };
}
