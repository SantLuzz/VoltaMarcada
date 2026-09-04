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
}
