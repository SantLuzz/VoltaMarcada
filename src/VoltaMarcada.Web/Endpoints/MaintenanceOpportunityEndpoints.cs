using VoltaMarcada.Application.MaintenanceOpportunities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Web.Authorization;
using VoltaMarcada.Web.Errors;

namespace VoltaMarcada.Web.Endpoints;

public static class MaintenanceOpportunityEndpoints
{
    public static IEndpointRouteBuilder MapMaintenanceOpportunityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/maintenance-opportunities").RequireAuthorization(AuthorizationPolicies.CompanyOwner);
        group.MapGet("/", async (MaintenanceOpportunityStatus? status, MaintenanceOpportunityUrgency? urgency, GetMaintenanceOpportunitiesUseCase useCase, CancellationToken cancellationToken) => (await useCase.ExecuteAsync(new GetMaintenanceOpportunitiesQuery(status, urgency), cancellationToken)).ToHttpResult(value => Results.Ok(value)));
        group.MapGet("/summary", async (GetMaintenanceOpportunitySummaryUseCase useCase, CancellationToken cancellationToken) => (await useCase.ExecuteAsync(cancellationToken)).ToHttpResult(value => Results.Ok(value)));
        group.MapGet("/{opportunityId:guid}/schedule-context", async (Guid opportunityId, GetMaintenanceOpportunityForSchedulingUseCase useCase, CancellationToken cancellationToken) => (await useCase.ExecuteAsync(opportunityId, cancellationToken)).ToHttpResult(value => Results.Ok(value)));
        group.MapPost("/{opportunityId:guid}/contacts/whatsapp", async (Guid opportunityId, PrepareMaintenanceOpportunityWhatsAppContactUseCase useCase, CancellationToken cancellationToken) => (await useCase.ExecuteAsync(opportunityId, cancellationToken)).ToHttpResult(value => Results.Ok(value)));
        group.MapPost("/{opportunityId:guid}/contacts/{contactId:guid}/sent", async (Guid opportunityId, Guid contactId, ConfirmMaintenanceOpportunityContactSentUseCase useCase, CancellationToken cancellationToken) => (await useCase.ExecuteAsync(opportunityId, contactId, cancellationToken)).ToHttpResult(() => Results.Ok()));
        group.MapPost("/{opportunityId:guid}/booking-request", async (Guid opportunityId, MarkMaintenanceOpportunityAsBookingRequestedUseCase useCase, CancellationToken cancellationToken) => (await useCase.ExecuteAsync(opportunityId, cancellationToken)).ToHttpResult(() => Results.Ok()));
        group.MapPost("/{opportunityId:guid}/complete", async (Guid opportunityId, CompleteMaintenanceOpportunityUseCase useCase, CancellationToken cancellationToken) => (await useCase.ExecuteAsync(opportunityId, cancellationToken)).ToHttpResult(() => Results.Ok()));
        group.MapPost("/{opportunityId:guid}/dismiss", async (Guid opportunityId, DismissMaintenanceOpportunityUseCase useCase, CancellationToken cancellationToken) => (await useCase.ExecuteAsync(opportunityId, cancellationToken)).ToHttpResult(() => Results.Ok()));
        return endpoints;
    }
}
