namespace VoltaMarcada.Application.Support;

public sealed record SupportRequestContext(string UserName, string UserEmail, string CompanyTradeName, string CompanyLegalName);

public interface ISupportRequestContextReader
{
    Task<SupportRequestContext?> GetAsync(Guid userId, Guid companyId, CancellationToken cancellationToken = default);
}

