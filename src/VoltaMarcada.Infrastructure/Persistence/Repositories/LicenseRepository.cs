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
        CancellationToken cancellationToken = default) =>
        await (from payment in dbContext.LicensePayments.AsNoTracking()
               join company in dbContext.Companies.AsNoTracking() on payment.CompanyId equals company.Id
               join membership in dbContext.CompanyUsers.AsNoTracking() on company.Id equals membership.CompanyId
               join user in dbContext.Users.AsNoTracking() on membership.UserId equals user.Id
               join license in dbContext.CompanyLicenses.AsNoTracking() on company.Id equals license.CompanyId
               where membership.Role == CompanyUserRole.Owner
               orderby payment.SubmittedAtUtc descending
               select new VoltaMarcada.Application.Licensing.PlatformLicensePaymentDto(
                   payment.Id, company.TradeName, user.Email ?? string.Empty, license.PaidUntilUtc,
                   payment.Amount, payment.SubmittedAtUtc, payment.Status, payment.DecidedAtUtc, payment.DecisionReason))
            .ToListAsync(cancellationToken);
    public Task<LicensePayment?> GetPaymentForPlatformAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default) =>
        dbContext.LicensePayments.AsNoTracking().SingleOrDefaultAsync(payment => payment.Id == paymentId, cancellationToken);
}
