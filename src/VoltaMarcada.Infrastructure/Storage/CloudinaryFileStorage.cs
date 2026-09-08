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
        var publicId = request.FileName ?? Guid.NewGuid().ToString("N");
        var storedPublicId = IsRawDocument(request.File.ContentType)
            ? (await cloudinary.UploadAsync(
                new RawUploadParams
                {
                    File = new FileDescription(request.File.FileName, request.File.Content),
                    Folder = request.Folder,
                    PublicId = publicId,
                    UniqueFilename = false,
                    Overwrite = false,
                    Type = request.Access == FileStorageAccess.Private ? "authenticated" : null
                },
                string.Empty,
                cancellationToken)).PublicId
            : (await cloudinary.UploadAsync(
                new ImageUploadParams
                {
                    File = new FileDescription(request.File.FileName, request.File.Content),
                    Folder = request.Folder,
                    PublicId = publicId,
                    UniqueFilename = false,
                    Overwrite = false,
                    Type = request.Access == FileStorageAccess.Private ? "authenticated" : null
                },
                cancellationToken)).PublicId;

        if (string.IsNullOrWhiteSpace(storedPublicId))
        {
            throw new InvalidOperationException("Cloudinary did not return a public ID for the uploaded file.");
        }

        return new FileStorageUploadResult(storedPublicId);
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
                ResourceType = IsRawDocumentKey(key) ? ResourceType.Raw : ResourceType.Image,
                Type = IsPrivatePaymentKey(key) ? "authenticated" : null
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

        var url = cloudinary.Api.UrlImgUp;
        if (IsRawDocumentKey(key))
        {
            url = url.ResourceType("raw");
        }
        if (IsPrivatePaymentKey(key))
        {
            url = url.Action("authenticated").Signed(true);
        }

        var fileUrl = url.BuildUrl(key);
        if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("Cloudinary did not generate a valid delivery URL.");
        }

        return uri;
    }

    private static bool IsPrivatePaymentKey(string key) =>
        key.StartsWith("pagamentos/", StringComparison.Ordinal);

    private static bool IsRawDocument(string contentType) =>
        string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase);

    private static bool IsRawDocumentKey(string key) =>
        Path.GetExtension(key).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
}
