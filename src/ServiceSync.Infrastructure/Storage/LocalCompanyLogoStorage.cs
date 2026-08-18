using ServiceSync.Application.Abstractions.Storage;

namespace ServiceSync.Infrastructure.Storage;

public sealed class LocalCompanyLogoStorage : ICompanyLogoStorage
{
    private const long MaximumFileSize = 5 * 1024 * 1024;
    private readonly string rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "company-logos");

    public async Task<CompanyLogoStorageResult> SaveAsync(CompanyLogoUpload upload, CancellationToken cancellationToken)
    {
        var extension = upload.ContentType switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/webp" => ".webp",
            _ => null
        };

        if (extension is null || upload.Length is <= 0 or > MaximumFileSize)
        {
            return new CompanyLogoStorageResult(CompanyLogoStorageResultCode.InvalidFile, null);
        }

        Directory.CreateDirectory(rootPath);
        var fileName = Guid.NewGuid().ToString("N") + extension;
        var fullPath = Path.Combine(rootPath, fileName);
        await using var destination = File.Create(fullPath);
        await upload.Content.CopyToAsync(destination, cancellationToken);
        return new CompanyLogoStorageResult(CompanyLogoStorageResultCode.Success, $"uploads/company-logos/{fileName}");
    }

    public Task DeleteAsync(string logoKey, CancellationToken cancellationToken)
    {
        var prefix = "uploads/company-logos/";
        if (logoKey.StartsWith(prefix, StringComparison.Ordinal))
        {
            var path = Path.Combine(rootPath, Path.GetFileName(logoKey));
            if (File.Exists(path)) File.Delete(path);
        }
        return Task.CompletedTask;
    }
}
