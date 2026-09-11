using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.MaintenanceOpportunities;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.Exceptions;

namespace VoltaMarcada.Application.Payments;

public sealed record UpdateWorkOrderChargeInstallmentCommand(decimal Amount, PaymentMethod PaymentMethod, string? OtherPaymentMethodDescription);

public sealed class ConfirmWorkOrderChargeInstallmentUseCase(MaintenanceOpportunityOwnerAuthorization authorization, IWorkOrderChargeRepository chargeRepository, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<OperationResult> ExecuteAsync(Guid installmentId, CancellationToken cancellationToken = default)
    {
        var companyId = await authorization.GetOwnerCompanyIdAsync(cancellationToken);
        if (companyId is null) return OperationResult.Failure(WorkOrderErrors.Forbidden);
        var installment = await chargeRepository.GetInstallmentAsync(companyId.Value, installmentId, cancellationToken);
        if (installment is null) return OperationResult.Failure(WorkOrderErrors.NotFound);
        try { installment.Confirm(currentUser.UserId, timeProvider.GetUtcNow()); }
        catch (DomainException) { return OperationResult.Failure(WorkOrderErrors.InvalidStatus); }
        await chargeRepository.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

public sealed class UpdateWorkOrderChargeInstallmentUseCase(MaintenanceOpportunityOwnerAuthorization authorization, IWorkOrderChargeRepository chargeRepository, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<OperationResult> ExecuteAsync(Guid installmentId, UpdateWorkOrderChargeInstallmentCommand command, CancellationToken cancellationToken = default)
    {
        var companyId = await authorization.GetOwnerCompanyIdAsync(cancellationToken);
        if (companyId is null) return OperationResult.Failure(WorkOrderErrors.Forbidden);
        var installment = await chargeRepository.GetInstallmentAsync(companyId.Value, installmentId, cancellationToken);
        if (installment is null) return OperationResult.Failure(WorkOrderErrors.NotFound);
        try { installment.Update(command.Amount, command.PaymentMethod, command.OtherPaymentMethodDescription, currentUser.UserId, timeProvider.GetUtcNow()); }
        catch (DomainException) { return OperationResult.Failure(WorkOrderErrors.InvalidInput); }
        await chargeRepository.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

public sealed class DeleteWorkOrderChargeInstallmentUseCase(MaintenanceOpportunityOwnerAuthorization authorization, IWorkOrderChargeRepository chargeRepository, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<OperationResult> ExecuteAsync(Guid installmentId, CancellationToken cancellationToken = default)
    {
        var companyId = await authorization.GetOwnerCompanyIdAsync(cancellationToken);
        if (companyId is null) return OperationResult.Failure(WorkOrderErrors.Forbidden);
        var installment = await chargeRepository.GetInstallmentAsync(companyId.Value, installmentId, cancellationToken);
        if (installment is null) return OperationResult.Failure(WorkOrderErrors.NotFound);
        try { installment.Delete(currentUser.UserId, timeProvider.GetUtcNow()); }
        catch (DomainException) { return OperationResult.Failure(WorkOrderErrors.InvalidStatus); }
        await chargeRepository.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}
