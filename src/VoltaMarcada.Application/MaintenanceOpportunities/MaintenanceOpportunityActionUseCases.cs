using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Exceptions;

namespace VoltaMarcada.Application.MaintenanceOpportunities;

public sealed record DiscardMaintenanceOpportunityCommand(string Reason);

public sealed class DiscardMaintenanceOpportunityUseCase(
    IMaintenanceOpportunityRepository repository,
    MaintenanceOpportunityOwnerAuthorization authorization,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public async Task<OperationResult> ExecuteAsync(
        Guid opportunityId,
        DiscardMaintenanceOpportunityCommand command,
        CancellationToken cancellationToken = default)
    {
        if (opportunityId == Guid.Empty || string.IsNullOrWhiteSpace(command.Reason))
        {
            return OperationResult.Failure(MaintenanceOpportunityErrors.InvalidInput);
        }

        var companyId = await authorization.GetOwnerCompanyIdAsync(cancellationToken);
        if (companyId is null)
        {
            return OperationResult.Failure(MaintenanceOpportunityErrors.Forbidden);
        }

        var opportunity = await repository.GetForCompanyAsync(opportunityId, companyId.Value, cancellationToken);
        if (opportunity is null)
        {
            return OperationResult.Failure(MaintenanceOpportunityErrors.NotFound);
        }

        try
        {
            opportunity.CloseWithoutConversion(currentUser.UserId, command.Reason, timeProvider.GetUtcNow());
            await repository.SaveChangesAsync(cancellationToken);
            return OperationResult.Success();
        }
        catch (DomainException)
        {
            return OperationResult.Failure(MaintenanceOpportunityErrors.InvalidStatus);
        }
    }
}
