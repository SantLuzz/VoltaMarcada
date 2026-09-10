namespace VoltaMarcada.Application.Abstractions.Storage;

public sealed record FileUpload(Stream Content, string FileName, string ContentType, long Length);

public enum FileStorageAccess
{
    Public,
    Private
}

public sealed record FileStorageUploadRequest(
    FileUpload File,
    string Folder,
    string? FileName = null,
    FileStorageAccess Access = FileStorageAccess.Public);

public sealed record FileStorageUploadResult(string Key);

public sealed record FileStorageDownloadResult(byte[] Content, string ContentType);

public interface IFileStorage
{
    Task<FileStorageUploadResult> UploadAsync(
        FileStorageUploadRequest request,
        CancellationToken cancellationToken);

    Task<FileStorageDownloadResult> GetAsync(string key, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
