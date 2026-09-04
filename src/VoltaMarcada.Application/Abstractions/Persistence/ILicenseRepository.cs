using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Abstractions.Persistence;

public sealed record CompanyLicenseMembership(
    CompanyUserRole Role,
    CompanyLicense License);

public interface ILicenseRepository
{
    Task<CompanyLicenseMembership?> GetMembershipAndLicenseAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<CompanyLicense?> GetForCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);
}
