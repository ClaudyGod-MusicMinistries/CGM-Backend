namespace ClaudyGod.Application.Features.Comments.DTOs;

// Public shape — never includes AuthorEmail, visitor emails are never shown
// to other visitors.
public record CommentDto(
    Guid Id,
    string AuthorName,
    string Content,
    Guid? ParentCommentId,
    DateTime CreatedAt);

// Admin/moderation shape — includes the email and enough post context to
// moderate without a second lookup.
public record AdminCommentDto(
    Guid Id,
    Guid BlogPostId,
    string BlogPostTitle,
    string BlogPostSlug,
    Guid? ParentCommentId,
    string AuthorName,
    string AuthorEmail,
    string Content,
    string Status,
    DateTime CreatedAt);

public record CreateCommentRequest(
    string AuthorName,
    string AuthorEmail,
    string Content,
    Guid? ParentCommentId = null,
    // Honeypot — a hidden field real visitors never fill. A non-empty value
    // here means a bot filled every field it could find; the handler
    // silently no-ops (returns as if it succeeded) rather than erroring, so
    // a bot can't distinguish "rejected" from "accepted" and learn to adapt.
    string? Website = null);

public record UpdateCommentStatusRequest(string Status);

public record SetReactionRequest(string VisitorToken, string Emoji);

public record RemoveReactionRequest(string VisitorToken);

public record ReactionSummaryDto(Dictionary<string, int> Counts, string? YourReaction);
