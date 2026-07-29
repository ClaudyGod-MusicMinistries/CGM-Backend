namespace ClaudyGod.Domain.Entities;

public static class ReactionEmoji
{
    // Fixed, server-validated set — never trust an arbitrary client-supplied
    // string here (spam/injection risk, and an open set defeats the point of
    // a Facebook-style fixed reaction palette).
    public static readonly string[] Allowed = ["👍", "❤️", "😂", "😮", "😢", "😠"];

    public static bool IsValid(string emoji) => Allowed.Contains(emoji);
}

public class Reaction : BaseEntity
{
    // Exactly one of these two is set — validated at the command layer, not
    // just left to convention (see SetReactionCommandValidator).
    public Guid? BlogPostId { get; private set; }
    public BlogPost? BlogPost { get; private set; }
    public Guid? CommentId { get; private set; }
    public Comment? Comment { get; private set; }

    public string VisitorToken { get; private set; } = string.Empty;
    public string Emoji { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    protected Reaction() { }

    public static Reaction ForPost(Guid blogPostId, string visitorToken, string emoji) =>
        new()
        {
            BlogPostId = blogPostId,
            VisitorToken = visitorToken.Trim(),
            Emoji = emoji,
            CreatedAt = DateTime.UtcNow
        };

    public static Reaction ForComment(Guid commentId, string visitorToken, string emoji) =>
        new()
        {
            CommentId = commentId,
            VisitorToken = visitorToken.Trim(),
            Emoji = emoji,
            CreatedAt = DateTime.UtcNow
        };

    public void ChangeEmoji(string emoji) => Emoji = emoji;
}
