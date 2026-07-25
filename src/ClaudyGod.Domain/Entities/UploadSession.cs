using ClaudyGod.Domain.Enums;
using ClaudyGod.Domain.Exceptions;

namespace ClaudyGod.Domain.Entities;

/// <summary>
/// Tracks one presigned-upload attempt end to end — issued at request-upload
/// time, flipped to Uploaded only after a real S3 HeadObject confirms the
/// bytes actually landed (see WebsiteS3StorageService.ConfirmUploadAsync).
/// This is what makes the pipeline non-silent: a client can never claim an
/// upload succeeded without the backend independently verifying it.
/// </summary>
public class UploadSession : AuditableEntity
{
    public UploadAssetKind AssetKind { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public string StorageBucket { get; private set; } = string.Empty;
    public string StorageKey { get; private set; } = string.Empty;
    public UploadSessionStatus Status { get; private set; } = UploadSessionStatus.Issued;
    public long? DeclaredFileSizeBytes { get; private set; }
    public long? ActualFileSizeBytes { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string RequestedBy { get; private set; } = string.Empty;

    protected UploadSession() { }

    public static UploadSession Issue(UploadAssetKind assetKind, string originalFileName,
        string mimeType, string storageBucket, string storageKey, long declaredFileSizeBytes,
        string requestedBy, DateTime expiresAt) =>
        new()
        {
            AssetKind = assetKind,
            OriginalFileName = originalFileName,
            MimeType = mimeType,
            StorageBucket = storageBucket,
            StorageKey = storageKey,
            DeclaredFileSizeBytes = declaredFileSizeBytes,
            RequestedBy = requestedBy,
            ExpiresAt = expiresAt
        };

    public void MarkUploaded(long actualFileSizeBytes)
    {
        if (Status != UploadSessionStatus.Issued && Status != UploadSessionStatus.Uploaded)
            throw new DomainException($"Cannot confirm an upload session in status '{Status}'.");

        Status = UploadSessionStatus.Uploaded;
        ActualFileSizeBytes = actualFileSizeBytes;
        CompletedAt ??= DateTime.UtcNow;
    }

    public void MarkExpired() => Status = UploadSessionStatus.Expired;
    public void MarkFailed() => Status = UploadSessionStatus.Failed;

    public bool IsExpired(DateTime now) => now > ExpiresAt && Status == UploadSessionStatus.Issued;
}
