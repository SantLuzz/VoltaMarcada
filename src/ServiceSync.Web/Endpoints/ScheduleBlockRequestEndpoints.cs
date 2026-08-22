using ServiceSync.Application.Scheduling;
using ServiceSync.Web.Authorization;

namespace ServiceSync.Web.Endpoints;

public static class ScheduleBlockRequestEndpoints
{
    public static IEndpointRouteBuilder MapScheduleBlockRequestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var requests = endpoints.MapGroup("/schedule-block-requests").RequireAuthorization(AuthorizationPolicies.CompanyMember);
        requests.MapGet("/", GetAsync);
        requests.MapPost("/", CreateAsync);
        requests.MapPut("/{requestId:guid}/approve", ApproveAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner);
        requests.MapPut("/{requestId:guid}/reject", RejectAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner);
        return endpoints;
    }

    private static async Task<IResult> GetAsync(GetScheduleBlockRequestsUseCase useCase, CancellationToken cancellationToken) => Results.Ok(await useCase.ExecuteAsync(cancellationToken));
    private static async Task<IResult> CreateAsync(CreateScheduleBlockRequestRequest request, CreateScheduleBlockRequestUseCase useCase, CancellationToken cancellationToken) => AppointmentEndpoints.ToResult(await useCase.ExecuteAsync(new CreateScheduleBlockRequestCommand(request.Type, request.StartDate, request.EndDate, request.IsFullDay, request.StartTime, request.EndTime, request.Notes), cancellationToken), id => Results.Created($"/schedule-block-requests/{id}", new { requestId = id }));
    private static async Task<IResult> ApproveAsync(Guid requestId, ApproveScheduleBlockRequestUseCase useCase, CancellationToken cancellationToken) => AppointmentEndpoints.ToResult(await useCase.ExecuteAsync(requestId, cancellationToken), _ => Results.Ok());
    private static async Task<IResult> RejectAsync(Guid requestId, RejectScheduleBlockRequestRequest request, RejectScheduleBlockRequestUseCase useCase, CancellationToken cancellationToken) => AppointmentEndpoints.ToResult(await useCase.ExecuteAsync(requestId, request.Reason, cancellationToken), _ => Results.Ok());

    public sealed record CreateScheduleBlockRequestRequest(ServiceSync.Domain.Enums.ScheduleBlockType Type, DateOnly StartDate, DateOnly EndDate, bool IsFullDay, TimeOnly? StartTime, TimeOnly? EndTime, string? Notes);
    public sealed record RejectScheduleBlockRequestRequest(string? Reason);
}
