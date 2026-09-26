namespace Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string originalFileName, CancellationToken cancellationToken = default);
    Task<Stream?> GetFileAsync(string storagePath, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default);
    string GetAbsolutePath(string storagePath);
}
