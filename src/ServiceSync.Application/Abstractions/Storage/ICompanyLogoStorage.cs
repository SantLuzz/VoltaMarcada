namespace ServiceSync.Application.Abstractions.Storage;

public sealed record CompanyLogoUpload(Stream Content, string FileName, string ContentType, long Length);

public sealed record CompanyLogoStorageResult(CompanyLogoStorageResultCode Code, string? LogoKey);

public enum CompanyLogoStorageResultCode
{
    Success,
    InvalidFile
}

public interface ICompanyLogoStorage
{
    Task<CompanyLogoStorageResult> SaveAsync(CompanyLogoUpload upload, CancellationToken cancellationToken);

    Task DeleteAsync(string logoKey, CancellationToken cancellationToken);
}
