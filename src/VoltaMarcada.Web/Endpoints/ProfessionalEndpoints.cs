using VoltaMarcada.Application.Professionals;
using VoltaMarcada.Web.Authorization;
using VoltaMarcada.Web.Errors;

namespace VoltaMarcada.Web.Endpoints;

public static class ProfessionalEndpoints
{
    public static IEndpointRouteBuilder MapProfessionalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var professionals = endpoints.MapGroup("/professionals")
            .RequireAuthorization(AuthorizationPolicies.CompanyMember);

        professionals.MapGet("/me", GetCurrentAsync);
        professionals.MapGet("/", GetAllAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner);
        professionals.MapGet("/options", GetOptionsAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner);
        professionals.MapGet("/{professionalId:guid}/availability", GetAvailabilityAsync);
        professionals.MapPut("/{professionalId:guid}/availability", SaveAvailabilityAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner).RequireOperationalCompanyLicense();
        professionals.MapPost("/{professionalId:guid}/availability/blocks", CreateScheduleBlockAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner).RequireOperationalCompanyLicense();
        professionals.MapDelete("/{professionalId:guid}/availability/blocks/{scheduleBlockId:guid}", DeleteScheduleBlockAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner).RequireOperationalCompanyLicense();
        professionals.MapGet("/{professionalId:guid}", GetForEditAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner);
        professionals.MapPost("/", CreateAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner).RequireOperationalCompanyLicense();
        professionals.MapPut("/{professionalId:guid}", UpdateAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner).RequireOperationalCompanyLicense();
        professionals.MapPut("/{professionalId:guid}/status", ChangeStatusAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner).RequireOperationalCompanyLicense();

        return endpoints;
    }

    private static async Task<IResult> GetCurrentAsync(GetCurrentEmployeeProfessionalUseCase useCase, CancellationToken cancellationToken)
    {
        var professional = await useCase.ExecuteAsync(cancellationToken);
        return professional is null ? Results.NotFound() : Results.Ok(professional);
    }

    private static async Task<IResult> GetAllAsync(string? search, GetProfessionalsUseCase useCase, CancellationToken cancellationToken) =>
        Results.Ok(await useCase.ExecuteAsync(search, cancellationToken));

    private static async Task<IResult> GetOptionsAsync(Guid? professionalId, GetProfessionalRegistrationOptionsUseCase useCase, CancellationToken cancellationToken) =>
        Results.Ok(await useCase.ExecuteAsync(professionalId, cancellationToken));

    private static async Task<IResult> GetForEditAsync(Guid professionalId, GetProfessionalForEditUseCase useCase, CancellationToken cancellationToken)
    {
        var professional = await useCase.ExecuteAsync(professionalId, cancellationToken);
        return professional is null ? Results.NotFound() : Results.Ok(professional);
    }

    private static async Task<IResult> GetAvailabilityAsync(
        Guid professionalId,
        GetProfessionalAvailabilityUseCase useCase,
        CancellationToken cancellationToken)
    {
        var availability = await useCase.ExecuteAsync(professionalId, cancellationToken);
        return availability is null ? Results.NotFound() : Results.Ok(availability);
    }

    private static async Task<IResult> SaveAvailabilityAsync(
        Guid professionalId,
        SaveProfessionalAvailabilityRequest request,
        SaveProfessionalAvailabilityUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            professionalId,
            new SaveProfessionalAvailabilityCommand(
                (request.DailySchedules ?? [])
                    .Select(schedule => new SaveProfessionalDailyScheduleCommand(
                        schedule.Weekday,
                        schedule.EntryTime,
                        schedule.ExitTime,
                        schedule.LunchStartTime,
                        schedule.LunchEndTime))
                    .ToArray()),
            cancellationToken);
        return result.ToHttpResult(_ => Results.Ok());
    }

    private static async Task<IResult> CreateScheduleBlockAsync(
        Guid professionalId,
        CreateScheduleBlockRequest request,
        CreateScheduleBlockUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            professionalId,
            new CreateScheduleBlockCommand(
                request.Type,
                request.StartDate,
                request.EndDate,
                request.IsFullDay,
                request.StartTime,
                request.EndTime,
                request.Notes),
            cancellationToken);
        return result.ToHttpResult(
            scheduleBlockId => Results.Created(
                $"/professionals/{professionalId}/availability/blocks/{scheduleBlockId}",
                new { scheduleBlockId }));
    }

    private static async Task<IResult> DeleteScheduleBlockAsync(
        Guid professionalId,
        Guid scheduleBlockId,
        DeleteScheduleBlockUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(professionalId, scheduleBlockId, cancellationToken);
        return result.ToHttpResult(_ => Results.NoContent());
    }

    private static async Task<IResult> CreateAsync(ProfessionalRequest request, CreateProfessionalUseCase useCase, CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(new CreateProfessionalCommand(request.CompanyUserId, request.ServiceIds, request.TimeZoneId), cancellationToken);
        return result.ToHttpResult(professionalId =>
            Results.Created($"/professionals/{professionalId}", new { professionalId }));
    }

    private static async Task<IResult> UpdateAsync(Guid professionalId, ProfessionalRequest request, UpdateProfessionalUseCase useCase, CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(professionalId, new UpdateProfessionalCommand(request.CompanyUserId, request.ServiceIds, request.TimeZoneId), cancellationToken);
        return result.ToHttpResult(_ => Results.Ok());
    }

    private static async Task<IResult> ChangeStatusAsync(Guid professionalId, ProfessionalStatusRequest request, ChangeProfessionalStatusUseCase useCase, CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(professionalId, request.IsActive, cancellationToken);
        return result.ToHttpResult(_ => Results.Ok());
    }

    public sealed record ProfessionalRequest(Guid CompanyUserId, IReadOnlyList<Guid> ServiceIds, string? TimeZoneId);

    public sealed record ProfessionalStatusRequest(bool IsActive);

    public sealed record SaveProfessionalAvailabilityRequest(
        IReadOnlyList<SaveProfessionalDailyScheduleRequest> DailySchedules);

    public sealed record SaveProfessionalDailyScheduleRequest(
        DayOfWeek Weekday,
        TimeOnly EntryTime,
        TimeOnly ExitTime,
        TimeOnly? LunchStartTime,
        TimeOnly? LunchEndTime);

    public sealed record CreateScheduleBlockRequest(
        VoltaMarcada.Domain.Enums.ScheduleBlockType Type,
        DateOnly StartDate,
        DateOnly EndDate,
        bool IsFullDay,
        TimeOnly? StartTime,
        TimeOnly? EndTime,
        string? Notes);
}
