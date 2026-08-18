namespace ServiceSync.Application.Abstractions.Tenancy;

public interface ICurrentCompany
{
    Task<Guid> GetCompanyIdAsync(CancellationToken cancellationToken = default);
}
