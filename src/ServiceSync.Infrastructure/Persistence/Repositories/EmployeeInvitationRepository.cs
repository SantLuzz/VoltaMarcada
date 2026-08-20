using Microsoft.EntityFrameworkCore;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.EmployeeInvitations;
using ServiceSync.Domain.Entities;
using ServiceSync.Domain.Enums;

namespace ServiceSync.Infrastructure.Persistence.Repositories;

public sealed class EmployeeInvitationRepository(AppDbContext dbContext) : IEmployeeInvitationRepository
{
    public Task<bool> IsCurrentUserOwnerAsync(
        Guid userId,
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        dbContext.CompanyUsers.AsNoTracking().AnyAsync(
            companyUser => companyUser.UserId == userId &&
                           companyUser.CompanyId == companyId &&
                           companyUser.Role == CompanyUserRole.Owner,
            cancellationToken);

    public async Task<CompanyOverviewDto?> GetCompanyOverviewAsync(
        Guid companyId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var company = await dbContext.Companies.AsNoTracking()
            .Where(item => item.Id == companyId)
            .Select(item => new
            {
                item.LegalName,
                item.TradeName,
                item.Document,
                item.WhatsApp,
                item.PostalCode,
                item.Address,
                item.LogoKey,
                StateName = dbContext.States.Where(state => state.Id == item.StateId).Select(state => state.Name).Single(),
                CityName = dbContext.Cities.Where(city => city.Id == item.CityId).Select(city => city.Name).Single()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (company is null)
        {
            return null;
        }

        var invitations = await dbContext.EmployeeInvitations.AsNoTracking()
            .Where(invitation => invitation.CompanyId == companyId)
            .OrderByDescending(invitation => invitation.CreatedAtUtc)
            .Select(invitation => new InvitationProjection(
                invitation.Id,
                invitation.FullName,
                invitation.Email,
                invitation.Document,
                invitation.Phone,
                invitation.Address,
                invitation.ExpiresAtUtc,
                invitation.CreatedAtUtc,
                invitation.AcceptedAtUtc,
                invitation.RevokedAtUtc))
            .ToListAsync(cancellationToken);

        var employeeDtos = await dbContext.CompanyUsers.AsNoTracking()
            .Where(companyUser => companyUser.CompanyId == companyId)
            .Join(
                dbContext.Users.AsNoTracking(),
                companyUser => companyUser.UserId,
                user => user.Id,
                (companyUser, user) => new EmployeeDto(
                    companyUser.Id,
                    user.FullName ?? user.Email ?? string.Empty,
                    user.Email ?? string.Empty,
                    user.Document ?? string.Empty,
                    user.Phone ?? string.Empty,
                    user.Address ?? string.Empty,
                    companyUser.CreatedAtUtc,
                    companyUser.Role))
            .OrderBy(employee => employee.Role)
            .ThenBy(employee => employee.FullName)
            .ToArrayAsync(cancellationToken);

        var invitationDtos = invitations
            .Select(invitation => new EmployeeInvitationDto(
                invitation.Id,
                invitation.FullName,
                invitation.Email,
                invitation.Document,
                invitation.Phone,
                invitation.Address,
                invitation.ExpiresAtUtc,
                invitation.CreatedAtUtc,
                invitation.AcceptedAtUtc is null && invitation.RevokedAtUtc is null && invitation.ExpiresAtUtc > now,
                invitation.RevokedAtUtc is not null,
                invitation.AcceptedAtUtc is null && invitation.RevokedAtUtc is null && invitation.ExpiresAtUtc <= now,
                invitation.AcceptedAtUtc is not null))
            .ToArray();

        return new CompanyOverviewDto(
            company.LegalName,
            company.TradeName,
            company.Document,
            company.WhatsApp,
            company.PostalCode,
            company.Address,
            company.StateName,
            company.CityName,
            company.LogoKey,
            employeeDtos,
            invitationDtos);
    }

    public Task<bool> HasPendingEmailAsync(
        Guid companyId,
        string email,
        DateTimeOffset now,
        CancellationToken cancellationToken = default) =>
        dbContext.EmployeeInvitations.AsNoTracking().AnyAsync(
            invitation => invitation.CompanyId == companyId &&
                          invitation.Email == email &&
                          invitation.AcceptedAtUtc == null &&
                          invitation.RevokedAtUtc == null &&
                          invitation.ExpiresAtUtc > now,
            cancellationToken);

    public Task<bool> HasPendingOrAcceptedDocumentAsync(
        Guid companyId,
        string document,
        DateTimeOffset now,
        CancellationToken cancellationToken = default) =>
        dbContext.EmployeeInvitations.AsNoTracking().AnyAsync(
            invitation => invitation.CompanyId == companyId &&
                          invitation.Document == document &&
                          (invitation.AcceptedAtUtc != null ||
                           (invitation.RevokedAtUtc == null && invitation.ExpiresAtUtc > now)),
            cancellationToken);

    public Task<EmployeeInvitation?> GetByIdAsync(
        Guid invitationId,
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        dbContext.EmployeeInvitations.SingleOrDefaultAsync(
            invitation => invitation.Id == invitationId && invitation.CompanyId == companyId,
            cancellationToken);

    public Task<EmployeeInvitation?> GetPendingByTokenHashAsync(
        string tokenHash,
        DateTimeOffset now,
        CancellationToken cancellationToken = default) =>
        dbContext.EmployeeInvitations.AsNoTracking().SingleOrDefaultAsync(
            invitation => invitation.TokenHash == tokenHash &&
                          invitation.AcceptedAtUtc == null &&
                          invitation.RevokedAtUtc == null &&
                          invitation.ExpiresAtUtc > now,
            cancellationToken);

    public Task AddAsync(EmployeeInvitation invitation, CancellationToken cancellationToken = default) =>
        dbContext.EmployeeInvitations.AddAsync(invitation, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private sealed record InvitationProjection(
        Guid Id,
        string FullName,
        string Email,
        string Document,
        string Phone,
        string Address,
        DateTimeOffset ExpiresAtUtc,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? AcceptedAtUtc,
        DateTimeOffset? RevokedAtUtc);
}
