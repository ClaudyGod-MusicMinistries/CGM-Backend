namespace ClaudyGod.Application.Common.Models;

public record FileUploadResult(
    string RelativePath,
    string FileName,
    string ContentType,
    long SizeBytes,
    string PublicUrl
);
