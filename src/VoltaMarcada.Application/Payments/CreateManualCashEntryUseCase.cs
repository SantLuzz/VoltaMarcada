using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.MaintenanceOpportunities;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Exceptions;

namespace VoltaMarcada.Application.Payments;

public sealed class CreateManualCashEntryUseCase(MaintenanceOpportunityOwnerAuthorization authorization, IWorkOrderChargeRepository chargeRepository, IWorkOrderRepository workOrderRepository, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<OperationResult> ExecuteAsync(CreateManualCashEntryCommand command, CancellationToken cancellationToken = default)
    {
        var companyId = await authorization.GetOwnerCompanyIdAsync(cancellationToken);
        if (companyId is null) return OperationResult.Failure(WorkOrderErrors.Forbidden);
        if (command.Amount <= 0 || command.BusinessDate == DateOnly.MinValue || !Enum.IsDefined(command.PaymentMethod)) return OperationResult.Failure(WorkOrderErrors.InvalidInput);
        if (command.WorkOrderId is { } workOrderId && await workOrderRepository.GetForAccessAsync(workOrderId, companyId.Value, null, cancellationToken) is null) return OperationResult.Failure(WorkOrderErrors.NotFound);
        try
        {
            await chargeRepository.AddAsync(WorkOrderCharge.CreateManual(companyId.Value, command.WorkOrderId, command.Amount, command.PaymentMethod, command.OtherPaymentMethodDescription, command.BusinessDate, currentUser.UserId, timeProvider.GetUtcNow()), cancellationToken);
            await chargeRepository.SaveChangesAsync(cancellationToken);
            return OperationResult.Success();
        }
        catch (DomainException)
        {
            return OperationResult.Failure(WorkOrderErrors.InvalidInput);
        }
    }
}