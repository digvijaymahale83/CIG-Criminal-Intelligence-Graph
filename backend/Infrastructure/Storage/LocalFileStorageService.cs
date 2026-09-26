using Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _baseStoragePath;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
    {
        _logger = logger;
        var configuredPath = configuration["Storage:LocalPath"] ?? "uploads/evidence";

        _baseStoragePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(Directory.GetCurrentDirectory(), "..", configuredPath);

        if (!Directory.Exists(_baseStoragePath))
        {
            Directory.CreateDirectory(_baseStoragePath);
        }
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string originalFileName, CancellationToken cancellationToken = default)
    {
        if (fileStream == null) throw new ArgumentNullException(nameof(fileStream));
        if (string.IsNullOrWhiteSpace(originalFileName)) throw new ArgumentException("Filename required", nameof(originalFileName));

        var sanitizedFileName = Path.GetFileName(originalFileName);
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            sanitizedFileName = sanitizedFileName.Replace(c, '_');
        }

        var uniquePrefix = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}";
        var targetFileName = $"{uniquePrefix}_{sanitizedFileName}";
        var relativePath = Path.Combine(DateTime.UtcNow.ToString("yyyy-MM"), targetFileName);
        var fullPath = Path.Combine(_baseStoragePath, relativePath);

        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        await using var destinationStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
        await fileStream.CopyToAsync(destinationStream, cancellationToken);

        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        _logger.LogInformation("Saved evidence file to: {FullPath}", fullPath);
        return relativePath.Replace('\\', '/');
    }

    public Task<Stream?> GetFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var sanitized = relativePath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_baseStoragePath, sanitized);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var sanitized = relativePath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_baseStoragePath, sanitized);

        if (File.Exists(fullPath))
        {
            try
            {
                File.Delete(fullPath);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to delete file {FullPath}: {Message}", fullPath, ex.Message);
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(false);
    }

    public string GetAbsolutePath(string storagePath)
    {
        if (Path.IsPathRooted(storagePath))
        {
            return storagePath;
        }
        var sanitized = storagePath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(_baseStoragePath, sanitized);
    }
}
