using Microsoft.EntityFrameworkCore;
using Npgsql;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Infrastructure.Persistence.Repositories;

public sealed class LicenseRepository(AppDbContext dbContext) : ILicenseRepository
{
    private const string PendingPaymentConstraintName = "UX_LicensePayments_CompanyId_Pending";

    public Task<CompanyLicenseMembership?> GetMembershipAndLicenseAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        (from membership in dbContext.CompanyUsers.AsNoTracking()
         join license in dbContext.CompanyLicenses.AsNoTracking()
             on membership.CompanyId equals license.CompanyId
         where membership.UserId == userId
         select new CompanyLicenseMembership(membership.Role, license))
        .SingleOrDefaultAsync(cancellationToken);

    public Task<CompanyLicense?> GetForCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        dbContext.CompanyLicenses
            .AsNoTracking()
            .SingleOrDefaultAsync(license => license.CompanyId == companyId, cancellationToken);

    public Task<bool> HasPendingPaymentAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        dbContext.LicensePayments.AnyAsync(
            payment => payment.CompanyId == companyId &&
                payment.Status == LicensePaymentStatus.PendingReview,
            cancellationToken);

    public Task<LicensePayment?> GetPendingPaymentAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        dbContext.LicensePayments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                payment => payment.CompanyId == companyId &&
                    payment.Status == LicensePaymentStatus.PendingReview,
                cancellationToken);

    public Task<LicensePayment?> GetPaymentAsync(
        Guid paymentId,
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        dbContext.LicensePayments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                payment => payment.Id == paymentId && payment.CompanyId == companyId,
                cancellationToken);

    public async Task<LicensePaymentPersistenceOutcome> AddPaymentAsync(
        LicensePayment payment,
        CancellationToken cancellationToken = default)
    {
        await dbContext.LicensePayments.AddAsync(payment, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return LicensePaymentPersistenceOutcome.Success;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: PendingPaymentConstraintName
            })
        {
            dbContext.Entry(payment).State = EntityState.Detached;
            return LicensePaymentPersistenceOutcome.PendingPaymentAlreadyExists;
        }
    }
    public async Task<LicensePaymentReviewPersistenceOutcome> ReviewPaymentAsync(
        Guid paymentId,
        Guid administratorId,
        VoltaMarcada.Application.Licensing.LicensePaymentReviewCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var payment = await dbContext.LicensePayments.SingleOrDefaultAsync(
            candidate => candidate.Id == paymentId && candidate.Status == LicensePaymentStatus.PendingReview,
            cancellationToken);
        if (payment is null)
        {
            return LicensePaymentReviewPersistenceOutcome.NotPending;
        }

        var license = await dbContext.CompanyLicenses.SingleOrDefaultAsync(
            candidate => candidate.CompanyId == payment.CompanyId,
            cancellationToken);
        if (license is null)
        {
            return LicensePaymentReviewPersistenceOutcome.NotPending;
        }

        if (command.Decision == VoltaMarcada.Application.Licensing.LicensePaymentReviewDecision.Approve)
        {
            payment.Approve(administratorId, now);
            license.Renew(now);
        }
        else
        {
            payment.Reject(administratorId, command.Reason!, now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return LicensePaymentReviewPersistenceOutcome.Success;
    }
    public async Task<IReadOnlyList<VoltaMarcada.Application.Licensing.PlatformLicensePaymentDto>> GetPlatformPaymentsAsync(
        CancellationToken cancellationToken = default)
    {
        var companies = await (from company in dbContext.Companies.AsNoTracking()
                               join licenseCandidate in dbContext.CompanyLicenses.AsNoTracking()
                                   on company.Id equals licenseCandidate.CompanyId into licenses
                               from license in licenses.DefaultIfEmpty()
                               join membershipCandidate in dbContext.CompanyUsers.AsNoTracking()
                                   on company.Id equals membershipCandidate.CompanyId into memberships
                               from membership in memberships.Where(candidate => candidate.Role == CompanyUserRole.Owner).DefaultIfEmpty()
                               join ownerCandidate in dbContext.Users.AsNoTracking()
                                   on membership.UserId equals ownerCandidate.Id into owners
                               from owner in owners.DefaultIfEmpty()
                               select new
                               {
                                   company.Id,
                                   company.TradeName,
                                   OwnerEmail = owner == null ? string.Empty : owner.Email ?? string.Empty,
                                   OwnerName = owner == null ? null : owner.FullName,
                                   LicenseStatus = license == null ? (CompanyLicenseStatus?)null : license.Status,
                                   PaidUntilUtc = license == null ? null : license.PaidUntilUtc,
                                   TrialEndsAtUtc = license == null ? null : (DateTimeOffset?)license.TrialEndsAtUtc
                               })
            .ToListAsync(cancellationToken);

        var payments = await (from payment in dbContext.LicensePayments.AsNoTracking()
                              join reviewerCandidate in dbContext.Users.AsNoTracking()
                                  on payment.DecidedByUserId equals reviewerCandidate.Id into reviewers
                              from reviewer in reviewers.DefaultIfEmpty()
                              orderby payment.SubmittedAtUtc descending
                              select new
                              {
                                  payment.Id,
                                  payment.CompanyId,
                                  payment.Amount,
                                  payment.SubmittedAtUtc,
                                  Status = (LicensePaymentStatus?)payment.Status,
                                  payment.DecidedAtUtc,
                                  payment.DecisionReason,
                                  ReviewerName = reviewer == null ? null : reviewer.FullName
                              })
            .ToListAsync(cancellationToken);

        var latestPayments = payments
            .GroupBy(payment => payment.CompanyId)
            .ToDictionary(group => group.Key, group => group.First());

        return companies
            .Select(company =>
            {
                latestPayments.TryGetValue(company.Id, out var payment);
                return new VoltaMarcada.Application.Licensing.PlatformLicensePaymentDto(
                    payment?.Id ?? Guid.Empty,
                    company.TradeName,
                    company.OwnerEmail,
                    company.PaidUntilUtc,
                    payment?.Amount ?? 0m,
                    payment?.SubmittedAtUtc ?? default,
                    payment?.Status,
                    payment?.DecidedAtUtc,
                    payment?.DecisionReason,
                    company.OwnerName,
                    payment?.ReviewerName,
                    company.LicenseStatus,
                    company.TrialEndsAtUtc);
            })
            .OrderBy(company => company.CompanyName)
            .ToList();
    }

    public Task<LicensePayment?> GetPaymentForPlatformAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default) =>
        dbContext.LicensePayments.AsNoTracking().SingleOrDefaultAsync(payment => payment.Id == paymentId, cancellationToken);
}
