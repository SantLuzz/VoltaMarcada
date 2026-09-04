using VoltaMarcada.Application.Equipment;
using VoltaMarcada.Web.Authorization;
using VoltaMarcada.Web.Errors;

namespace VoltaMarcada.Web.Endpoints;

public static class EquipmentEndpoints
{
    public static IEndpointRouteBuilder MapEquipmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var equipment = endpoints.MapGroup("/equipment")
            .RequireAuthorization(AuthorizationPolicies.CompanyMember);

        equipment.MapPost("/", CreateAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner).RequireOperationalCompanyLicense();
        equipment.MapGet("/", GetAllAsync);
        equipment.MapGet("/{equipmentId:guid}/edit", GetForEditAsync);
        equipment.MapPut("/{equipmentId:guid}", UpdateAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner).RequireOperationalCompanyLicense();
        equipment.MapDelete("/{equipmentId:guid}", DeleteAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner).RequireOperationalCompanyLicense();

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        EquipmentRequest request,
        CreateEquipmentUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(ToCreateCommand(request), cancellationToken);
        return result.ToHttpResult(equipmentId =>
            Results.Created($"/equipment/{equipmentId}/edit", new { equipmentId }));
    }

    private static async Task<IResult> GetAllAsync(
        string? search,
        GetEquipmentUseCase useCase,
        CancellationToken cancellationToken) =>
        Results.Ok(await useCase.ExecuteAsync(new GetEquipmentQuery(search), cancellationToken));

    private static async Task<IResult> GetForEditAsync(
        Guid equipmentId,
        GetEquipmentForEditUseCase useCase,
        CancellationToken cancellationToken)
    {
        var equipment = await useCase.ExecuteAsync(equipmentId, cancellationToken);
        return equipment is null ? Results.NotFound() : Results.Ok(equipment);
    }

    private static async Task<IResult> UpdateAsync(
        Guid equipmentId,
        EquipmentRequest request,
        UpdateEquipmentUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(equipmentId, ToUpdateCommand(request), cancellationToken);
        return result.ToHttpResult(_ => Results.Ok());
    }

    private static async Task<IResult> DeleteAsync(
        Guid equipmentId,
        DeleteEquipmentUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(equipmentId, cancellationToken);
        return result.ToHttpResult(_ => Results.NoContent());
    }

    private static CreateEquipmentCommand ToCreateCommand(EquipmentRequest request) =>
        new(
            request.CustomerId,
            request.Type,
            request.Manufacturer,
            request.Model,
            request.SerialNumber,
            request.Capacity,
            request.LocationDescription,
            request.InstallationDate,
            request.Notes);

    private static UpdateEquipmentCommand ToUpdateCommand(EquipmentRequest request) =>
        new(
            request.CustomerId,
            request.Type,
            request.Manufacturer,
            request.Model,
            request.SerialNumber,
            request.Capacity,
            request.LocationDescription,
            request.InstallationDate,
            request.Notes);

    public sealed record EquipmentRequest(
        Guid CustomerId,
        string Type,
        string? Manufacturer,
        string? Model,
        string? SerialNumber,
        string? Capacity,
        string? LocationDescription,
        DateOnly? InstallationDate,
        string? Notes);
}
