namespace ClaudyGod.Application.Common.Models;

public record PresignedUploadResult(
    Guid SessionId,
    string PresignedUrl,
    string Key,
    string Bucket,
    DateTime ExpiresAt);

public record ConfirmedUploadResult(
    Guid SessionId,
    string Key,
    string PublicUrl,
    string ContentType,
    long FileSizeBytes,
    DateTime UploadedAt);

/// <summary>Result of a direct server-side write (payment slips) — no presign/confirm round-trip, the bytes are already in hand.</summary>
public record UploadedFileResult(string Key, string PublicUrl, long FileSizeBytes);
