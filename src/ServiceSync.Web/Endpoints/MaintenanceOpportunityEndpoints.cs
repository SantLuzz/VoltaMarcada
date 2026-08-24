using ServiceSync.Application.MaintenanceOpportunities;
using ServiceSync.Domain.Enums;
using ServiceSync.Web.Authorization;
using ServiceSync.Web.Errors;

namespace ServiceSync.Web.Endpoints;

public static class MaintenanceOpportunityEndpoints
{
    public static IEndpointRouteBuilder MapMaintenanceOpportunityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/maintenance-opportunities").RequireAuthorization(AuthorizationPolicies.CompanyOwner);
        group.MapGet("/", async (MaintenanceOpportunityStatus? status, MaintenanceOpportunityUrgency? urgency, GetMaintenanceOpportunitiesUseCase useCase, CancellationToken cancellationToken) => (await useCase.ExecuteAsync(new GetMaintenanceOpportunitiesQuery(status, urgency), cancellationToken)).ToHttpResult(value => Results.Ok(value)));
        group.MapGet("/summary", async (GetMaintenanceOpportunitySummaryUseCase useCase, CancellationToken cancellationToken) => (await useCase.ExecuteAsync(cancellationToken)).ToHttpResult(value => Results.Ok(value)));
        group.MapGet("/{opportunityId:guid}/schedule-context", async (Guid opportunityId, GetMaintenanceOpportunityForSchedulingUseCase useCase, CancellationToken cancellationToken) => (await useCase.ExecuteAsync(opportunityId, cancellationToken)).ToHttpResult(value => Results.Ok(value)));
        group.MapPost("/{opportunityId:guid}/contact", async (Guid opportunityId, MarkMaintenanceOpportunityAsContactedUseCase useCase, CancellationToken cancellationToken) => (await useCase.ExecuteAsync(opportunityId, cancellationToken)).ToHttpResult(value => Results.Ok(value)));
        group.MapPost("/{opportunityId:guid}/booking-request", async (Guid opportunityId, MarkMaintenanceOpportunityAsBookingRequestedUseCase useCase, CancellationToken cancellationToken) => (await useCase.ExecuteAsync(opportunityId, cancellationToken)).ToHttpResult(value => Results.Ok(value)));
        group.MapPost("/{opportunityId:guid}/complete", async (Guid opportunityId, CompleteMaintenanceOpportunityUseCase useCase, CancellationToken cancellationToken) => (await useCase.ExecuteAsync(opportunityId, cancellationToken)).ToHttpResult(value => Results.Ok(value)));
        group.MapPost("/{opportunityId:guid}/dismiss", async (Guid opportunityId, DismissMaintenanceOpportunityUseCase useCase, CancellationToken cancellationToken) => (await useCase.ExecuteAsync(opportunityId, cancellationToken)).ToHttpResult(value => Results.Ok(value)));
        return endpoints;
    }
}
