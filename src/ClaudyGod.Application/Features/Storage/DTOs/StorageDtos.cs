using ClaudyGod.Domain.Enums;

namespace ClaudyGod.Application.Features.Storage.DTOs;

public record RequestUploadRequest(
    UploadAssetKind Kind,
    string FileName,
    string MimeType,
    long FileSizeBytes);

public record ConfirmUploadRequest(Guid SessionId);
