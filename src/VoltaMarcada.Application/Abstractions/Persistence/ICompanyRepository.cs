using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Application.Abstractions.Persistence;

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Company?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<CompanyQuotePdfProfile?> GetQuotePdfProfileAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<CompanyQuotePdfProfile?> GetQuotePdfProfileAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task AddAsync(Company company, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed record CompanyQuotePdfProfile(
    Guid CompanyId,
    string LegalName,
    string TradeName,
    string Document,
    string WhatsApp,
    string PostalCode,
    string Address,
    string CityName,
    string StateAbbreviation,
    string? LogoKey);

public sealed record CompanyQuotePdfProfile(
    Guid CompanyId,
    string LegalName,
    string TradeName,
    string Document,
    string WhatsApp,
    string PostalCode,
    string Address,
    string CityName,
    string StateAbbreviation,
    string? LogoKey);
