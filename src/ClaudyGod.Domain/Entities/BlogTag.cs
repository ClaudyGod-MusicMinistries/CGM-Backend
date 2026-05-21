namespace ClaudyGod.Domain.Entities;

public class BlogTag : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public ICollection<BlogPostTag> PostTags { get; private set; } = [];

    protected BlogTag() { }

    public static BlogTag Create(string name, string slug) =>
        new() { Name = name.Trim(), Slug = slug.ToLowerInvariant().Trim() };
}
