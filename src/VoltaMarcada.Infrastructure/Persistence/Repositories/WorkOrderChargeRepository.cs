using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Entities;

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

    public async Task AddAsync(WorkOrderCharge charge, CancellationToken cancellationToken = default)
    {
        await dbContext.WorkOrderCharges.AddAsync(charge, cancellationToken);
        foreach (var definition in charge.Installments)
        {
            await dbContext.WorkOrderChargeInstallments.AddAsync(
                WorkOrderChargeInstallment.Create(charge.CompanyId, charge.Id, Guid.NewGuid(), definition.PaymentMethod, definition.Amount, definition.OtherPaymentMethodDescription, charge.CreatedAtUtc),
                cancellationToken);
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);
}


