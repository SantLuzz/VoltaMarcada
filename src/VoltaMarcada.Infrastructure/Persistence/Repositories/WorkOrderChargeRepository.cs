using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Infrastructure.Persistence.Repositories;

public sealed class WorkOrderChargeRepository(AppDbContext dbContext) : IWorkOrderChargeRepository
{
    public Task<WorkOrderCharge?> GetForWorkOrderAsync(Guid companyId, Guid workOrderId, CancellationToken cancellationToken = default) =>
        dbContext.WorkOrderCharges.SingleOrDefaultAsync(charge => charge.CompanyId == companyId && charge.WorkOrderId == workOrderId, cancellationToken);

    public Task<WorkOrderChargeInstallment?> GetInstallmentAsync(Guid companyId, Guid installmentId, CancellationToken cancellationToken = default) =>
        dbContext.WorkOrderChargeInstallments.SingleOrDefaultAsync(installment => installment.CompanyId == companyId && installment.Id == installmentId, cancellationToken);

    public async Task<IReadOnlyList<WorkOrderChargeInstallment>> GetInstallmentsForWorkOrderAsync(Guid companyId, Guid workOrderId, CancellationToken cancellationToken = default) =>
        await (from installment in dbContext.WorkOrderChargeInstallments
               join charge in dbContext.WorkOrderCharges on installment.WorkOrderChargeId equals charge.Id
               where installment.CompanyId == companyId && charge.WorkOrderId == workOrderId
               orderby installment.CreatedAtUtc
               select installment).ToArrayAsync(cancellationToken);

    public Task<IReadOnlyList<CashEntryPersistenceModel>> GetDailyEntriesAsync(Guid companyId, DateOnly businessDate, CancellationToken cancellationToken = default) =>
        GetEntriesAsync(companyId, businessDate, null, cancellationToken);

    public Task<IReadOnlyList<CashEntryPersistenceModel>> GetPendingEntriesAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        GetEntriesAsync(companyId, null, WorkOrderChargeInstallmentStatus.Pending, cancellationToken);

    public async Task AddAsync(WorkOrderCharge charge, CancellationToken cancellationToken = default)
    {
        await dbContext.WorkOrderCharges.AddAsync(charge, cancellationToken);
        foreach (var definition in charge.Installments)
        {
            var installment = WorkOrderChargeInstallment.Create(charge.CompanyId, charge.Id, Guid.NewGuid(), definition.PaymentMethod, definition.Amount, definition.OtherPaymentMethodDescription, charge.CreatedAtUtc);
            if (charge.IsManualEntry)
            {
                installment.Confirm(charge.CreatedByUserId!.Value, charge.CreatedAtUtc);
            }

            await dbContext.WorkOrderChargeInstallments.AddAsync(installment, cancellationToken);
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);

    private async Task<IReadOnlyList<CashEntryPersistenceModel>> GetEntriesAsync(Guid companyId, DateOnly? businessDate, WorkOrderChargeInstallmentStatus? status, CancellationToken cancellationToken)
    {
        var query = from installment in dbContext.WorkOrderChargeInstallments.AsNoTracking()
                    join charge in dbContext.WorkOrderCharges.AsNoTracking() on installment.WorkOrderChargeId equals charge.Id
                    where installment.CompanyId == companyId && charge.CompanyId == companyId && installment.Status != WorkOrderChargeInstallmentStatus.Deleted
                    select new { Installment = installment, Charge = charge };

        if (businessDate is { } selectedDate) query = query.Where(entry => entry.Charge.BusinessDate == selectedDate);
        if (status is { } currentStatus) query = query.Where(entry => entry.Installment.Status == currentStatus);

        return await query
            .OrderBy(entry => entry.Charge.BusinessDate)
            .ThenBy(entry => entry.Installment.CreatedAtUtc)
            .Select(entry => new CashEntryPersistenceModel(
                entry.Installment.Id,
                entry.Charge.WorkOrderId,
                entry.Charge.BusinessDate,
                entry.Installment.Amount,
                entry.Installment.PaymentMethod,
                entry.Installment.OtherPaymentMethodDescription,
                entry.Installment.Status,
                entry.Installment.CreatedAtUtc,
                entry.Installment.PaidAtUtc))
            .ToArrayAsync(cancellationToken);
    }
}
