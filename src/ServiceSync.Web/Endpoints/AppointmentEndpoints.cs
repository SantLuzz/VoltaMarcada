using ServiceSync.Application.Scheduling;
using ServiceSync.Web.Authorization;

namespace ServiceSync.Web.Endpoints;

public static class AppointmentEndpoints
{
    public static IEndpointRouteBuilder MapAppointmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var appointments = endpoints.MapGroup("/appointments").RequireAuthorization(AuthorizationPolicies.CompanyMember);
        appointments.MapGet("/", GetAsync);
        appointments.MapGet("/slots", GetSlotsAsync);
        appointments.MapPost("/", CreateAsync);
        appointments.MapPost("/{appointmentId:guid}/start", StartAsync);
        appointments.MapPut("/{appointmentId:guid}/reschedule", RescheduleAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner);
        appointments.MapPut("/{appointmentId:guid}/professional", ReassignAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner);
        appointments.MapPut("/{appointmentId:guid}/adjust", AdjustAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner);
        appointments.MapPut("/{appointmentId:guid}/complete", CompleteAsync);
        appointments.MapDelete("/{appointmentId:guid}", CancelAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner);
        return endpoints;
    }

    private static async Task<IResult> GetAsync(DateTimeOffset startAtUtc, DateTimeOffset endAtUtc, GetAppointmentsUseCase useCase, CancellationToken cancellationToken) => Results.Ok(await useCase.ExecuteAsync(startAtUtc, endAtUtc, cancellationToken));
    private static async Task<IResult> GetSlotsAsync(Guid professionalId, DateOnly date, int durationMinutes, GetAvailableAppointmentSlotsUseCase useCase, CancellationToken cancellationToken) => Results.Ok(await useCase.ExecuteAsync(professionalId, date, durationMinutes, cancellationToken: cancellationToken));
    private static async Task<IResult> CreateAsync(CreateAppointmentRequest request, CreateAppointmentUseCase useCase, CancellationToken cancellationToken) => ToResult(await useCase.ExecuteAsync(new CreateAppointmentCommand(request.ProfessionalId, request.Date, request.StartTime, request.DurationMinutes, request.CustomerId, request.EquipmentId, request.ServiceId, request.WorkOrderId, request.Notes), cancellationToken), id => Results.Created($"/appointments/{id}", new { appointmentId = id }));
    private static async Task<IResult> StartAsync(Guid appointmentId, StartAppointmentUseCase useCase, CancellationToken cancellationToken) => ToResult(await useCase.ExecuteAsync(appointmentId, cancellationToken), _ => Results.Ok());
    private static async Task<IResult> RescheduleAsync(Guid appointmentId, RescheduleAppointmentRequest request, RescheduleAppointmentUseCase useCase, CancellationToken cancellationToken) => ToResult(await useCase.ExecuteAsync(appointmentId, new RescheduleAppointmentCommand(request.Date, request.StartTime, request.Reason), cancellationToken), _ => Results.Ok());
    private static async Task<IResult> ReassignAsync(Guid appointmentId, ReassignAppointmentRequest request, ReassignAppointmentUseCase useCase, CancellationToken cancellationToken) => ToResult(await useCase.ExecuteAsync(appointmentId, request.ProfessionalId, cancellationToken), _ => Results.Ok());
    private static async Task<IResult> AdjustAsync(Guid appointmentId, AdjustAppointmentRequest request, AdjustAppointmentUseCase useCase, CancellationToken cancellationToken) => ToResult(await useCase.ExecuteAsync(appointmentId, new AdjustAppointmentCommand(request.ProfessionalId, request.Date, request.StartTime, request.Reason, request.WorkOrderId), cancellationToken), _ => Results.Ok());
    private static async Task<IResult> CompleteAsync(Guid appointmentId, CompleteAppointmentUseCase useCase, CancellationToken cancellationToken) => ToResult(await useCase.ExecuteAsync(appointmentId, cancellationToken), _ => Results.Ok());
    private static async Task<IResult> CancelAsync(Guid appointmentId, CancelAppointmentUseCase useCase, CancellationToken cancellationToken) => ToResult(await useCase.ExecuteAsync(appointmentId, cancellationToken), _ => Results.NoContent());

    internal static IResult ToResult(SchedulingOperationResult result, Func<Guid, IResult> success) => result.Code switch
    {
        SchedulingOperationResultCode.Success when result.Id is { } id => success(id),
        SchedulingOperationResultCode.InvalidInput => Results.BadRequest(new { error = "Revise os dados do agendamento." }),
        SchedulingOperationResultCode.NotFound => Results.NotFound(),
        SchedulingOperationResultCode.Forbidden => Results.Forbid(),
        SchedulingOperationResultCode.ProfessionalUnavailable or SchedulingOperationResultCode.AppointmentConflict => Results.Conflict(new { error = "O profissional não está disponível nesse horário." }),
        SchedulingOperationResultCode.ProfessionalNotQualified => Results.BadRequest(new { error = "O profissional não está habilitado para este serviço." }),
        SchedulingOperationResultCode.InvalidStatus => Results.BadRequest(new { error = "O agendamento não permite essa ação." }),
        SchedulingOperationResultCode.BlockRequestRequiresAppointmentResolution => Results.Conflict(new { error = "Existem agendamentos que precisam ser ajustados antes da aprovação." }),
        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    public sealed record CreateAppointmentRequest(Guid ProfessionalId, DateOnly Date, TimeOnly StartTime, int DurationMinutes, Guid? CustomerId, Guid? EquipmentId, Guid? ServiceId, Guid? WorkOrderId, string? Notes);
    public sealed record RescheduleAppointmentRequest(DateOnly Date, TimeOnly StartTime, string Reason);
    public sealed record ReassignAppointmentRequest(Guid ProfessionalId);
    public sealed record AdjustAppointmentRequest(Guid ProfessionalId, DateOnly Date, TimeOnly StartTime, string Reason, Guid? WorkOrderId);
}
