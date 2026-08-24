namespace ServiceSync.Application.Abstractions.Tenancy;

public interface ICurrentCompany
{
    Task<Guid?> FindCompanyIdAsync(CancellationToken cancellationToken = default);

    Task<Guid> GetCompanyIdAsync(CancellationToken cancellationToken = default);
}
