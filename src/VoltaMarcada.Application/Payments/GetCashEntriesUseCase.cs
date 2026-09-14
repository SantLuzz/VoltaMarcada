using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.MaintenanceOpportunities;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Payments;

public sealed class GetCashEntriesUseCase(MaintenanceOpportunityOwnerAuthorization authorization, IWorkOrderChargeRepository chargeRepository)
{
    public async Task<OperationResult<CashEntriesDto>> ExecuteAsync(GetCashEntriesQuery query, CancellationToken cancellationToken = default)
    {
        var companyId = await authorization.GetOwnerCompanyIdAsync(cancellationToken);
        if (companyId is null) return OperationResult<CashEntriesDto>.Failure(WorkOrderErrors.Forbidden);
        if (!query.PendingOnly && query.BusinessDate is null) return OperationResult<CashEntriesDto>.Failure(WorkOrderErrors.InvalidInput);

        var entries = query.PendingOnly
            ? await chargeRepository.GetPendingEntriesAsync(companyId.Value, cancellationToken)
            : await chargeRepository.GetDailyEntriesAsync(companyId.Value, query.BusinessDate!.Value, cancellationToken);
        var resultEntries = entries.Select(entry => new CashEntryDto(entry.InstallmentId, entry.WorkOrderId, entry.BusinessDate, entry.Amount, entry.PaymentMethod, entry.OtherPaymentMethodDescription, entry.Status, entry.CreatedAtUtc, entry.PaidAtUtc)).ToArray();
        var summary = new CashSummaryDto(
            resultEntries.Sum(entry => entry.Amount),
            resultEntries.Where(entry => entry.Status == WorkOrderChargeInstallmentStatus.Pending).Sum(entry => entry.Amount),
            resultEntries.Where(entry => entry.Status == WorkOrderChargeInstallmentStatus.Paid).Sum(entry => entry.Amount));
        return OperationResult<CashEntriesDto>.Success(new CashEntriesDto(query.PendingOnly ? null : query.BusinessDate, query.PendingOnly, summary, resultEntries));
    }
}