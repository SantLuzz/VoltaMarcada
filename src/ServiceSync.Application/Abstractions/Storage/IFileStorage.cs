namespace ServiceSync.Application.Abstractions.Storage;

public sealed record FileUpload(Stream Content, string FileName, string ContentType, long Length);

public sealed record FileStorageUploadRequest(FileUpload File, string Folder);

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
