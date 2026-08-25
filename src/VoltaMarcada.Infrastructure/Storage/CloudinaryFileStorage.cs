using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using VoltaMarcada.Application.Abstractions.Storage;

namespace VoltaMarcada.Infrastructure.Storage;

public sealed class CloudinaryFileStorage(
    Cloudinary cloudinary,
    HttpClient httpClient) : IFileStorage
{
    public async Task<FileStorageUploadResult> UploadAsync(
        FileStorageUploadRequest request,
        CancellationToken cancellationToken)
    {
        var upload = await cloudinary.UploadAsync(
            new ImageUploadParams
            {
                File = new FileDescription(request.File.FileName, request.File.Content),
                Folder = request.Folder,
                PublicId = Guid.NewGuid().ToString("N")
            },
            cancellationToken);

        if (string.IsNullOrWhiteSpace(upload.PublicId))
        {
            throw new InvalidOperationException("Cloudinary did not return a public ID for the uploaded file.");
        }

        return new FileStorageUploadResult(upload.PublicId);
    }

    public async Task<FileStorageDownloadResult> GetAsync(string key, CancellationToken cancellationToken)
    {
        var fileUrl = BuildFileUrl(cloudinary, key);
        using var response = await httpClient.GetAsync(fileUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        return new FileStorageDownloadResult(content, contentType);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await cloudinary.DestroyAsync(
            new DeletionParams(key)
            {
                ResourceType = ResourceType.Image
            });

        if (!string.Equals(result.Result, "ok", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(result.Result, "not found", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cloudinary did not delete the requested file.");
        }
    }

    private static Uri BuildFileUrl(Cloudinary cloudinary, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("File key is required.", nameof(key));
        }

        var fileUrl = cloudinary.Api.UrlImgUp.BuildUrl(key);
        if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("Cloudinary did not generate a valid delivery URL.");
        }

        return uri;
    }
}
