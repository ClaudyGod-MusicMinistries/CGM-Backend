using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClaudyGod.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly string _baseUrl;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(IConfiguration config, ILogger<FileStorageService> logger)
    {
        _logger = logger;
        _basePath = Path.GetFullPath(config["FileStorage:LocalBasePath"] ?? "uploads");
        _baseUrl = config["FileStorage:BaseUrl"] ?? "/uploads";
        Directory.CreateDirectory(_basePath);
    }

    public async Task<FileUploadResult> SaveAsync(IFormFile file, string category,
        string[] allowedExtensions, long maxSizeBytes, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
            throw new ValidationException($"File type '{extension}' is not allowed.");

        if (file.Length > maxSizeBytes)
            throw new ValidationException(
                $"File exceeds maximum size of {maxSizeBytes / 1024 / 1024} MB.");

        var fileName = $"{Guid.NewGuid()}{extension}";
        var categoryDir = Path.Combine(_basePath, category);
        Directory.CreateDirectory(categoryDir);

        var fullPath = Path.Combine(categoryDir, fileName);
        var relativePath = Path.Combine(category, fileName).Replace('\\', '/');

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream, ct);

        _logger.LogInformation("Saved file {FileName} to {Path}", fileName, relativePath);

        return new FileUploadResult(relativePath, file.FileName, file.ContentType,
            file.Length, GetPublicUrl(relativePath));
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, relativePath.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("Deleted file {Path}", relativePath);
        }

        return Task.CompletedTask;
    }

    public string GetPublicUrl(string relativePath) =>
        $"{_baseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";

    public Task<Stream> GetStreamAsync(string relativePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, relativePath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(fullPath))
            throw new NotFoundException($"File not found: {relativePath}");

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return Task.FromResult(stream);
    }
}
