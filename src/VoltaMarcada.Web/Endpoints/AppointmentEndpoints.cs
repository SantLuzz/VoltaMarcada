using VoltaMarcada.Application.Scheduling;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Web.Authorization;
using VoltaMarcada.Web.Errors;

namespace VoltaMarcada.Web.Endpoints;

public static class AppointmentEndpoints
{
    public static IEndpointRouteBuilder MapAppointmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var appointments = endpoints.MapGroup("/appointments").RequireAuthorization(AuthorizationPolicies.CompanyMember);
        appointments.MapGet("/", GetAsync);
        appointments.MapGet("/slots", GetSlotsAsync);
        appointments.MapPost("/", CreateAsync).RequireOperationalCompanyLicense();
        appointments.MapPost("/{appointmentId:guid}/start", StartAsync).RequireOperationalCompanyLicense();
        appointments.MapPut("/{appointmentId:guid}/reschedule", RescheduleAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner).RequireOperationalCompanyLicense();
        appointments.MapPut("/{appointmentId:guid}/professional", ReassignAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner).RequireOperationalCompanyLicense();
        appointments.MapPut("/{appointmentId:guid}/adjust", AdjustAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner).RequireOperationalCompanyLicense();
        appointments.MapPut("/{appointmentId:guid}/complete", CompleteAsync).RequireOperationalCompanyLicense();
        appointments.MapDelete("/{appointmentId:guid}", CancelAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner).RequireOperationalCompanyLicense();
        return endpoints;
    }

    private static async Task<IResult> GetAsync(DateTimeOffset startAtUtc, DateTimeOffset endAtUtc, GetAppointmentsUseCase useCase, CancellationToken cancellationToken) => Results.Ok(await useCase.ExecuteAsync(startAtUtc, endAtUtc, cancellationToken));
    private static async Task<IResult> GetSlotsAsync(Guid professionalId, DateOnly date, int durationMinutes, GetAvailableAppointmentSlotsUseCase useCase, CancellationToken cancellationToken) => Results.Ok(await useCase.ExecuteAsync(professionalId, date, durationMinutes, cancellationToken: cancellationToken));
    private static async Task<IResult> CreateAsync(CreateAppointmentRequest request, CreateAppointmentUseCase useCase, CancellationToken cancellationToken) => (await useCase.ExecuteAsync(new CreateAppointmentCommand(request.ProfessionalId, request.Date, request.StartTime, request.DurationMinutes, request.CustomerId, request.EquipmentId, request.ServiceId, request.WorkOrderId, request.Notes, request.MaintenanceOpportunityId, request.Type, request.Description), cancellationToken)).ToHttpResult(id => Results.Created($"/appointments/{id}", new { appointmentId = id }));
    private static async Task<IResult> StartAsync(Guid appointmentId, StartAppointmentUseCase useCase, CancellationToken cancellationToken) => (await useCase.ExecuteAsync(appointmentId, cancellationToken)).ToHttpResult(_ => Results.Ok());
    private static async Task<IResult> RescheduleAsync(Guid appointmentId, RescheduleAppointmentRequest request, RescheduleAppointmentUseCase useCase, CancellationToken cancellationToken) => (await useCase.ExecuteAsync(appointmentId, new RescheduleAppointmentCommand(request.Date, request.StartTime, request.Reason), cancellationToken)).ToHttpResult(_ => Results.Ok());
    private static async Task<IResult> ReassignAsync(Guid appointmentId, ReassignAppointmentRequest request, ReassignAppointmentUseCase useCase, CancellationToken cancellationToken) => (await useCase.ExecuteAsync(appointmentId, request.ProfessionalId, cancellationToken)).ToHttpResult(_ => Results.Ok());
    private static async Task<IResult> AdjustAsync(Guid appointmentId, AdjustAppointmentRequest request, AdjustAppointmentUseCase useCase, CancellationToken cancellationToken) => (await useCase.ExecuteAsync(appointmentId, new AdjustAppointmentCommand(request.ProfessionalId, request.Date, request.StartTime, request.Reason, request.WorkOrderId), cancellationToken)).ToHttpResult(_ => Results.Ok());
    private static async Task<IResult> CompleteAsync(Guid appointmentId, CompleteAppointmentUseCase useCase, CancellationToken cancellationToken) => (await useCase.ExecuteAsync(appointmentId, cancellationToken)).ToHttpResult(_ => Results.Ok());
    private static async Task<IResult> CancelAsync(Guid appointmentId, CancelAppointmentUseCase useCase, CancellationToken cancellationToken) => (await useCase.ExecuteAsync(appointmentId, cancellationToken)).ToHttpResult(_ => Results.NoContent());

    public sealed record CreateAppointmentRequest(Guid ProfessionalId, DateOnly Date, TimeOnly StartTime, int DurationMinutes, Guid? CustomerId, Guid? EquipmentId, Guid? ServiceId, Guid? WorkOrderId, string? Notes, AppointmentType Type, string? Description = null, Guid? MaintenanceOpportunityId = null);
    public sealed record RescheduleAppointmentRequest(DateOnly Date, TimeOnly StartTime, string Reason);
    public sealed record ReassignAppointmentRequest(Guid ProfessionalId);
    public sealed record AdjustAppointmentRequest(Guid ProfessionalId, DateOnly Date, TimeOnly StartTime, string Reason, Guid? WorkOrderId);
}
