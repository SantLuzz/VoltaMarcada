using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.MaintenanceOpportunities;
using VoltaMarcada.Domain.Exceptions;

namespace VoltaMarcada.Application.PublicBooking;

public sealed class RevokeMaintenanceBookingLinkUseCase(
    MaintenanceOpportunityOwnerAuthorization authorization,
    IMaintenanceOpportunityRepository repository,
    TimeProvider timeProvider)
{
    public async Task<OperationResult> ExecuteAsync(Guid opportunityId, CancellationToken cancellationToken = default)
    {
        var companyId = await authorization.GetOwnerCompanyIdAsync(cancellationToken);
        if (companyId is null) return OperationResult.Failure(MaintenanceOpportunityErrors.Forbidden);
        var opportunity = await repository.GetForCompanyAsync(opportunityId, companyId.Value, cancellationToken);
        if (opportunity is null) return OperationResult.Failure(MaintenanceOpportunityErrors.NotFound);
        try
        {
            opportunity.RevokeBookingToken(timeProvider.GetUtcNow());
        }
        catch (DomainException)
        {
            return OperationResult.Failure(MaintenanceOpportunityErrors.InvalidStatus);
        }
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}
