using ClaudyGod.Application.Common.Models;
using ClaudyGod.Domain.Enums;

namespace ClaudyGod.Application.Common.Interfaces;

/// <summary>
/// The website's S3-backed upload pipeline — deliberately not shaped like
/// the old IFileStorageService (which assumed a synchronous server-side
/// IFormFile write). A presigned-URL flow has three distinct steps handled
/// by three distinct methods; CreatePresignedUploadAsync and
/// ConfirmUploadAsync are never called by the same request.
/// </summary>
public interface IWebsiteStorageService
{
    /// <summary>Validates against the asset kind's policy, issues an UploadSession row, returns a presigned PUT URL.</summary>
    Task<PresignedUploadResult> CreatePresignedUploadAsync(UploadAssetKind kind, string fileName,
        string mimeType, long declaredFileSizeBytes, string requestedBy, CancellationToken ct = default);

    /// <summary>Real integrity check (S3 HeadObject) — throws if the object was never actually uploaded.</summary>
    Task<ConfirmedUploadResult> ConfirmUploadAsync(Guid sessionId, string requestedBy, CancellationToken ct = default);

    string GetPublicUrl(string key);

    Task DeleteAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Direct server-side write for flows that already have the bytes in hand via a
    /// synchronous multipart request (payment-proof slips) — no presign/confirm
    /// round-trip needed since there's no separate client-to-S3 step to verify.
    /// </summary>
    Task<UploadedFileResult> UploadServerSideAsync(Stream content, UploadAssetKind kind,
        string fileName, string mimeType, CancellationToken ct = default);
}
