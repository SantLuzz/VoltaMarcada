using ServiceSync.Application.EmployeeInvitations;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Application.Abstractions.Persistence;

public interface IEmployeeInvitationRepository
{
    Task<bool> IsCurrentUserOwnerAsync(
        Guid userId,
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<CompanyOverviewDto?> GetCompanyOverviewAsync(
        Guid companyId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<bool> HasPendingEmailAsync(
        Guid companyId,
        string email,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<bool> HasPendingOrAcceptedDocumentAsync(
        Guid companyId,
        string document,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<EmployeeInvitation?> GetByIdAsync(
        Guid invitationId,
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<Guid?> GetUserIdByMembershipIdAsync(
        Guid membershipId,
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<EmployeeInvitation?> GetPendingByTokenHashAsync(
        string tokenHash,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task AddAsync(EmployeeInvitation invitation, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
