using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;

namespace VoltaMarcada.Application.MaintenanceOpportunities;

public sealed class GetMaintenanceOpportunityForSchedulingUseCase(
    IMaintenanceOpportunityRepository repository,
    MaintenanceOpportunityOwnerAuthorization authorization)
{
    public async Task<OperationResult<MaintenanceOpportunityScheduleContext>> ExecuteAsync(
        Guid opportunityId,
        CancellationToken cancellationToken = default)
    {
        if (opportunityId == Guid.Empty)
        {
            return OperationResult<MaintenanceOpportunityScheduleContext>.Failure(MaintenanceOpportunityErrors.InvalidInput);
        }

        var companyId = await authorization.GetOwnerCompanyIdAsync(cancellationToken);
        if (companyId is null)
        {
            return OperationResult<MaintenanceOpportunityScheduleContext>.Failure(MaintenanceOpportunityErrors.Forbidden);
        }

        var opportunity = await repository.GetForCompanyAsync(opportunityId, companyId.Value, cancellationToken);
        if (opportunity is null)
        {
            return OperationResult<MaintenanceOpportunityScheduleContext>.Failure(MaintenanceOpportunityErrors.NotFound);
        }

        if (opportunity.Status is VoltaMarcada.Domain.Enums.MaintenanceOpportunityStatus.Converted or VoltaMarcada.Domain.Enums.MaintenanceOpportunityStatus.ClosedWithoutConversion)
        {
            return OperationResult<MaintenanceOpportunityScheduleContext>.Failure(MaintenanceOpportunityErrors.InvalidStatus);
        }

        return OperationResult<MaintenanceOpportunityScheduleContext>.Success(
            new MaintenanceOpportunityScheduleContext(
                opportunity.Id,
                opportunity.CustomerId,
                opportunity.EquipmentId,
                opportunity.RecommendedServiceId));
    }
}
