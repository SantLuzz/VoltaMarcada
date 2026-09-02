using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.MaintenanceOpportunities;

public sealed class GetMaintenanceOpportunityForQuoteUseCase(
    IMaintenanceOpportunityRepository repository,
    MaintenanceOpportunityOwnerAuthorization authorization)
{
    public async Task<OperationResult<MaintenanceOpportunityQuoteContext>> ExecuteAsync(
        Guid opportunityId,
        CancellationToken cancellationToken = default)
    {
        if (opportunityId == Guid.Empty)
        {
            return OperationResult<MaintenanceOpportunityQuoteContext>.Failure(MaintenanceOpportunityErrors.InvalidInput);
        }

        var companyId = await authorization.GetOwnerCompanyIdAsync(cancellationToken);
        if (companyId is null)
        {
            return OperationResult<MaintenanceOpportunityQuoteContext>.Failure(MaintenanceOpportunityErrors.Forbidden);
        }

        var opportunity = await repository.GetForCompanyAsync(opportunityId, companyId.Value, cancellationToken);
        if (opportunity is null)
        {
            return OperationResult<MaintenanceOpportunityQuoteContext>.Failure(MaintenanceOpportunityErrors.NotFound);
        }

        if (opportunity.Status is MaintenanceOpportunityStatus.Converted or MaintenanceOpportunityStatus.ClosedWithoutConversion)
        {
            return OperationResult<MaintenanceOpportunityQuoteContext>.Failure(MaintenanceOpportunityErrors.InvalidStatus);
        }

        return OperationResult<MaintenanceOpportunityQuoteContext>.Success(
            new MaintenanceOpportunityQuoteContext(
                opportunity.Id,
                opportunity.CustomerId,
                opportunity.EquipmentId,
                opportunity.RecommendedServiceId));
    }
}
