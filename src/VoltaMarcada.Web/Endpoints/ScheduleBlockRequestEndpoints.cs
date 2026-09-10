using VoltaMarcada.Application.Scheduling;
using VoltaMarcada.Web.Authorization;
using VoltaMarcada.Web.Errors;

namespace VoltaMarcada.Web.Endpoints;

public static class ScheduleBlockRequestEndpoints
{
    public static IEndpointRouteBuilder MapScheduleBlockRequestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var requests = endpoints.MapGroup("/schedule-block-requests").RequireAuthorization(AuthorizationPolicies.CompanyMember);
        requests.MapGet("/", GetAsync);
        requests.MapPost("/", CreateAsync).RequireOperationalCompanyLicense();
        requests.MapPut("/{requestId:guid}/approve", ApproveAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner).RequireOperationalCompanyLicense();
        requests.MapPut("/{requestId:guid}/reject", RejectAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner).RequireOperationalCompanyLicense();
        return endpoints;
    }

    private static async Task<IResult> GetAsync(GetScheduleBlockRequestsUseCase useCase, CancellationToken cancellationToken) => Results.Ok(await useCase.ExecuteAsync(cancellationToken));
    private static async Task<IResult> CreateAsync(CreateScheduleBlockRequestRequest request, CreateScheduleBlockRequestUseCase useCase, CancellationToken cancellationToken) => (await useCase.ExecuteAsync(new CreateScheduleBlockRequestCommand(request.Type, request.StartDate, request.EndDate, request.IsFullDay, request.StartTime, request.EndTime, request.Notes), cancellationToken)).ToHttpResult(id => Results.Created($"/schedule-block-requests/{id}", new { requestId = id }));
    private static async Task<IResult> ApproveAsync(Guid requestId, ApproveScheduleBlockRequestUseCase useCase, CancellationToken cancellationToken) => (await useCase.ExecuteAsync(requestId, cancellationToken)).ToHttpResult(_ => Results.Ok());
    private static async Task<IResult> RejectAsync(Guid requestId, RejectScheduleBlockRequestRequest request, RejectScheduleBlockRequestUseCase useCase, CancellationToken cancellationToken) => (await useCase.ExecuteAsync(requestId, request.Reason, cancellationToken)).ToHttpResult(_ => Results.Ok());

    public sealed record CreateScheduleBlockRequestRequest(VoltaMarcada.Domain.Enums.ScheduleBlockType Type, DateOnly StartDate, DateOnly EndDate, bool IsFullDay, TimeOnly? StartTime, TimeOnly? EndTime, string? Notes);
    public sealed record RejectScheduleBlockRequestRequest(string? Reason);
}
