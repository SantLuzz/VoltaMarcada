using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.MaintenanceOpportunities;

namespace VoltaMarcada.Application.PublicBooking;

public sealed class GenerateMaintenanceBookingLinkUseCase(MaintenanceOpportunityOwnerAuthorization authorization, IMaintenanceOpportunityRepository repository, IAppointmentRequestRepository requestRepository, IPublicBookingConfiguration configuration, TimeProvider timeProvider)
{
    public async Task<OperationResult<string>> ExecuteAsync(Guid opportunityId, CancellationToken cancellationToken = default)
    {
        var companyId = await authorization.GetOwnerCompanyIdAsync(cancellationToken);
        if (companyId is null) return OperationResult<string>.Failure(MaintenanceOpportunityErrors.Forbidden);
        var opportunity = await repository.GetForCompanyAsync(opportunityId, companyId.Value, cancellationToken);
        if (opportunity is null) return OperationResult<string>.Failure(MaintenanceOpportunityErrors.NotFound);
        if (await requestRepository.HasOpenForMaintenanceOpportunityAsync(companyId.Value, opportunityId, cancellationToken))
            return OperationResult<string>.Failure(MaintenanceOpportunityErrors.InvalidStatus);
        var token = PublicTokenGenerator.Generate();
        try { opportunity.SetBookingToken(PublicTokenGenerator.Hash(token), timeProvider.GetUtcNow().AddDays(configuration.MaintenanceLinkLifetimeDays), timeProvider.GetUtcNow()); }
        catch (VoltaMarcada.Domain.Exceptions.DomainException) { return OperationResult<string>.Failure(MaintenanceOpportunityErrors.InvalidStatus); }
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult<string>.Success(token);
    }
}
