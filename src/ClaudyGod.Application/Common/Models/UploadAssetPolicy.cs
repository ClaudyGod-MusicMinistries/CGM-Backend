using ClaudyGod.Domain.Enums;

namespace ClaudyGod.Application.Common.Models;

public record UploadAssetPolicy(
    long MaxSizeBytes,
    string[] AllowedMimeTypes,
    string[] AllowedExtensions,
    string RecommendedFolder);

/// <summary>
/// Per-asset-kind MIME/extension/size validation, enforced in
/// WebsiteS3StorageService.CreatePresignedUploadAsync before a presigned URL
/// is ever issued. Mirrors the numbers already proven for the sibling mobile
/// app's upload pipeline (ClaudyGod-MobileApp/services/api), but with
/// website-scoped folder names since the two pipelines share nothing —
/// different bucket, different credentials, different key namespace.
/// </summary>
public static class UploadAssetPolicies
{
    public const long HardCapBytes = 512L * 1024 * 1024;

    public static readonly IReadOnlyDictionary<UploadAssetKind, UploadAssetPolicy> All =
        new Dictionary<UploadAssetKind, UploadAssetPolicy>
        {
            [UploadAssetKind.Thumbnail] = new(
                5L * 1024 * 1024,
                ["image/jpeg", "image/png", "image/webp"],
                [".jpg", ".jpeg", ".png", ".webp"],
                "website-thumbnails"),
            [UploadAssetKind.Audio] = new(
                150L * 1024 * 1024,
                ["audio/mpeg", "audio/mp3", "audio/mp4", "audio/x-m4a", "audio/aac", "audio/wav", "audio/x-wav", "audio/flac", "audio/x-flac", "audio/ogg", "audio/webm"],
                [".mp3", ".m4a", ".aac", ".wav", ".flac", ".ogg", ".webm"],
                "website-audio"),
            [UploadAssetKind.Video] = new(
                500L * 1024 * 1024,
                ["video/mp4", "video/quicktime", "video/webm", "video/x-matroska"],
                [".mp4", ".mov", ".webm", ".mkv"],
                "website-video"),
            [UploadAssetKind.Document] = new(
                10L * 1024 * 1024,
                ["application/pdf", "image/jpeg", "image/png"],
                [".pdf", ".jpg", ".jpeg", ".png"],
                "website-documents"),
        };

    public static UploadAssetPolicy For(UploadAssetKind kind) => All[kind];
}
