using ClaudyGod.Application.Common.Models;
using Microsoft.AspNetCore.Http;

namespace ClaudyGod.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<FileUploadResult> SaveAsync(IFormFile file, string category,
        string[] allowedExtensions, long maxSizeBytes, CancellationToken ct = default);

    Task DeleteAsync(string relativePath, CancellationToken ct = default);

    string GetPublicUrl(string relativePath);

    Task<Stream> GetStreamAsync(string relativePath, CancellationToken ct = default);
}
