using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Domain.Entities;
using ClaudyGod.Domain.Enums;
using ClaudyGod.Domain.Exceptions;
using ClaudyGod.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ValidationException = ClaudyGod.Domain.Exceptions.ValidationException;

namespace ClaudyGod.Infrastructure.Services;

/// <summary>
/// S3-compatible storage for website uploads (Supabase's S3 gateway, a
/// dedicated bucket/credential pair distinct from the sibling mobile app's
/// pipeline — see IWebsiteStorageService's doc comment for why this isn't
/// shaped like the retired IFileStorageService).
///
/// Cross-SDK trap, preempted rather than left to be rediscovered as a mystery
/// 401: recent AWS SDKs auto-attach a CRC32 checksum to presigned requests by
/// default, computed against an empty body at presign time — that checksum
/// then mismatches the real PUT body against non-AWS S3-compatible gateways
/// like Supabase's (which don't special-case this the way real AWS S3 does).
/// RequestChecksumCalculation/ResponseChecksumValidation = WHEN_REQUIRED
/// below restores the pre-default-integrity behavior.
/// </summary>
public class WebsiteS3StorageService : IWebsiteStorageService
{
    private static readonly TimeSpan PresignExpiry = TimeSpan.FromMinutes(15);

    private readonly IAmazonS3 _s3;
    private readonly WebsiteStorageOptions _options;
    private readonly IApplicationDbContext _db;
    private readonly ILogger<WebsiteS3StorageService> _logger;

    public WebsiteS3StorageService(IOptions<WebsiteStorageOptions> options,
        IApplicationDbContext db, ILogger<WebsiteS3StorageService> logger)
    {
        _options = options.Value;
        _db = db;
        _logger = logger;

        var config = new AmazonS3Config
        {
            ServiceURL = _options.S3Endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = string.IsNullOrWhiteSpace(_options.S3Region) ? "us-east-1" : _options.S3Region,
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };
        var credentials = new BasicAWSCredentials(_options.S3AccessKeyId, _options.S3SecretAccessKey);
        _s3 = new AmazonS3Client(credentials, config);
    }

    public async Task<PresignedUploadResult> CreatePresignedUploadAsync(UploadAssetKind kind,
        string fileName, string mimeType, long declaredFileSizeBytes, string requestedBy,
        CancellationToken ct = default)
    {
        var policy = UploadAssetPolicies.For(kind);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (!policy.AllowedExtensions.Contains(extension))
            throw new ValidationException(
                $"File type '{extension}' is not allowed for {kind} uploads. Allowed: {string.Join(", ", policy.AllowedExtensions)}.");

        if (!policy.AllowedMimeTypes.Contains(mimeType.ToLowerInvariant()))
            throw new ValidationException(
                $"MIME type '{mimeType}' is not allowed for {kind} uploads. Allowed: {string.Join(", ", policy.AllowedMimeTypes)}.");

        if (declaredFileSizeBytes <= 0 || declaredFileSizeBytes > policy.MaxSizeBytes || declaredFileSizeBytes > UploadAssetPolicies.HardCapBytes)
            throw new ValidationException(
                $"File exceeds the maximum size of {policy.MaxSizeBytes / 1024 / 1024} MB for {kind} uploads.");

        var key = BuildKey(policy.RecommendedFolder, fileName);
        var expiresAt = DateTime.UtcNow.Add(PresignExpiry);

        var session = UploadSession.Issue(kind, fileName, mimeType, _options.Bucket, key,
            declaredFileSizeBytes, requestedBy, expiresAt);
        _db.UploadSessions.Add(session);
        await _db.SaveChangesAsync(ct);

        var presignedUrl = _s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = expiresAt,
            ContentType = mimeType,
        });

        _logger.LogInformation("Issued upload session {SessionId} for key {Key} ({Kind}, requested by {RequestedBy})",
            session.Id, key, kind, requestedBy);

        return new PresignedUploadResult(session.Id, presignedUrl, key, _options.Bucket, expiresAt);
    }

    public async Task<ConfirmedUploadResult> ConfirmUploadAsync(Guid sessionId, string requestedBy,
        CancellationToken ct = default)
    {
        var session = await _db.UploadSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct)
            ?? throw new NotFoundException("Upload session", sessionId);

        if (session.RequestedBy != requestedBy)
            throw new DomainException("This upload session does not belong to the current user.");

        if (session.Status == UploadSessionStatus.Issued && session.IsExpired(DateTime.UtcNow))
        {
            session.MarkExpired();
            await _db.SaveChangesAsync(ct);
            throw new DomainException("This upload session has expired. Request a new upload and try again.");
        }

        // Real integrity check — the only source of truth for whether the PUT
        // actually landed. A client claiming success without this passing is
        // exactly the silent-failure mode this pipeline exists to prevent.
        GetObjectMetadataResponse metadata;
        try
        {
            metadata = await _s3.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = session.StorageBucket,
                Key = session.StorageKey,
            }, ct);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            session.MarkFailed();
            await _db.SaveChangesAsync(ct);
            throw new ValidationException(
                "The file was not found in storage — the upload did not complete. Please try uploading again before confirming.");
        }

        if (metadata.ContentLength != session.DeclaredFileSizeBytes)
        {
            await _s3.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = session.StorageBucket,
                Key = session.StorageKey,
            }, ct);
            session.MarkFailed();
            await _db.SaveChangesAsync(ct);
            throw new ValidationException(
                "The uploaded file size does not match the authorized upload. Please request a new upload session.");
        }

        session.MarkUploaded(metadata.ContentLength);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Confirmed upload session {SessionId} — {Bytes} bytes at {Key}",
            session.Id, metadata.ContentLength, session.StorageKey);

        return new ConfirmedUploadResult(session.Id, session.StorageKey, GetPublicUrl(session.StorageKey),
            session.MimeType, metadata.ContentLength, session.CompletedAt ?? DateTime.UtcNow);
    }

    public string GetPublicUrl(string key)
    {
        var baseUrl = _options.PublicBaseUrl.TrimEnd('/');
        var objectPath = key.TrimStart('/');

        // Support either a project origin or the complete public bucket URL.
        // This avoids duplicating Supabase's /storage/v1/object/public path when
        // operators configure the full URL shown in the Supabase dashboard.
        return baseUrl.Contains("/storage/v1/object/public/", StringComparison.OrdinalIgnoreCase)
            ? $"{baseUrl}/{objectPath}"
            : $"{baseUrl}/storage/v1/object/public/{_options.Bucket}/{objectPath}";
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        await _s3.DeleteObjectAsync(new DeleteObjectRequest { BucketName = _options.Bucket, Key = key }, ct);
        _logger.LogInformation("Deleted storage object {Key}", key);
    }

    public async Task<UploadedFileResult> UploadServerSideAsync(Stream content, UploadAssetKind kind,
        string fileName, string mimeType, CancellationToken ct = default)
    {
        var policy = UploadAssetPolicies.For(kind);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (!policy.AllowedExtensions.Contains(extension))
            throw new ValidationException(
                $"File type '{extension}' is not allowed for {kind} uploads. Allowed: {string.Join(", ", policy.AllowedExtensions)}.");

        if (content.Length > policy.MaxSizeBytes)
            throw new ValidationException(
                $"File exceeds the maximum size of {policy.MaxSizeBytes / 1024 / 1024} MB for {kind} uploads.");

        var key = BuildKey(policy.RecommendedFolder, fileName);

        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            InputStream = content,
            ContentType = mimeType,
            AutoCloseStream = false,
        }, ct);

        _logger.LogInformation("Server-side wrote {Key} ({Bytes} bytes)", key, content.Length);

        return new UploadedFileResult(key, GetPublicUrl(key), content.Length);
    }

    private static string BuildKey(string folder, string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var baseName = Path.GetFileNameWithoutExtension(originalFileName);
        var sanitized = Sanitize(baseName);
        var now = DateTime.UtcNow;

        return $"website/{folder}/{now:yyyy}/{now:MM}/{now:dd}/{Guid.NewGuid()}-{sanitized}{extension.ToLowerInvariant()}";
    }

    private static string Sanitize(string segment)
    {
        var lowered = segment.ToLowerInvariant();
        var chars = lowered.Select(c => char.IsLetterOrDigit(c) || c is '.' or '_' or '-' ? c : '-').ToArray();
        var result = new string(chars).Trim('-');
        return string.IsNullOrEmpty(result) ? "file" : result;
    }
}
