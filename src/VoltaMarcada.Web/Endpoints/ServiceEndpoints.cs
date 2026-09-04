using VoltaMarcada.Application.Services;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Web.Authorization;
using VoltaMarcada.Web.Errors;

namespace VoltaMarcada.Web.Endpoints;

public static class ServiceEndpoints
{
    public static IEndpointRouteBuilder MapServiceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var services = endpoints.MapGroup("/services")
            .RequireAuthorization(AuthorizationPolicies.CompanyMember);

        services.MapPost("/", CreateAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner).RequireOperationalCompanyLicense();
        services.MapGet("/", GetAllAsync);
        services.MapGet("/{serviceId:guid}/edit", GetForEditAsync);
        services.MapPut("/{serviceId:guid}", UpdateAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner).RequireOperationalCompanyLicense();
        services.MapDelete("/{serviceId:guid}", DeleteAsync).RequireAuthorization(AuthorizationPolicies.CompanyOwner).RequireOperationalCompanyLicense();

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        ServiceRequest? request,
        CreateServiceUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!HasValidChecklistItems(request))
        {
            return InvalidInput();
        }

        var result = await useCase.ExecuteAsync(ToCreateCommand(request!), cancellationToken);
        return result.ToHttpResult(serviceId =>
            Results.Created($"/services/{serviceId}/edit", new { serviceId }));
    }

    private static async Task<IResult> GetAllAsync(
        string? search,
        GetServicesUseCase useCase,
        CancellationToken cancellationToken) =>
        Results.Ok(await useCase.ExecuteAsync(new GetServicesQuery(search), cancellationToken));

    private static async Task<IResult> GetForEditAsync(
        Guid serviceId,
        GetServiceForEditUseCase useCase,
        CancellationToken cancellationToken)
    {
        var service = await useCase.ExecuteAsync(serviceId, cancellationToken);
        return service is null ? Results.NotFound() : Results.Ok(service);
    }

    private static async Task<IResult> UpdateAsync(
        Guid serviceId,
        ServiceRequest? request,
        UpdateServiceUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!HasValidChecklistItems(request))
        {
            return InvalidInput();
        }

        var result = await useCase.ExecuteAsync(serviceId, ToUpdateCommand(request!), cancellationToken);
        return result.ToHttpResult(_ => Results.Ok());
    }

    private static async Task<IResult> DeleteAsync(
        Guid serviceId,
        DeleteServiceUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(serviceId, cancellationToken);
        return result.ToHttpResult(_ => Results.NoContent());
    }

    private static IResult InvalidInput() =>
        OperationResultHttpExtensions.ValidationProblem(
            ServiceErrors.InvalidInput.Code,
            OperationErrorMessages.GetMessage(ServiceErrors.InvalidInput.Code));

    private static bool HasValidChecklistItems(ServiceRequest? request) =>
        request?.ChecklistItems is not null && request.ChecklistItems.All(item => item is not null);

    private static CreateServiceCommand ToCreateCommand(ServiceRequest request) =>
        new(
            request.Name,
            request.Description,
            request.DurationMinutes,
            request.PriceType,
            request.BasePrice,
            request.AllowOnlineBooking,
            request.RequireBeforePhotos,
            request.RequireAfterPhotos,
            ToChecklistItemInputs(request.ChecklistItems!));

    private static UpdateServiceCommand ToUpdateCommand(ServiceRequest request) =>
        new(
            request.Name,
            request.Description,
            request.DurationMinutes,
            request.PriceType,
            request.BasePrice,
            request.AllowOnlineBooking,
            request.RequireBeforePhotos,
            request.RequireAfterPhotos,
            ToChecklistItemInputs(request.ChecklistItems!));

    private static IReadOnlyList<ServiceChecklistItemInput> ToChecklistItemInputs(
        IReadOnlyList<ServiceChecklistItemRequest?> checklistItems) =>
        checklistItems
            .Select(item => new ServiceChecklistItemInput(item!.Description, item.IsRequired))
            .ToArray();

    public sealed record ServiceRequest(
        string Name,
        string? Description,
        int DurationMinutes,
        ServicePriceType PriceType,
        decimal? BasePrice,
        bool AllowOnlineBooking,
        bool RequireBeforePhotos,
        bool RequireAfterPhotos,
        IReadOnlyList<ServiceChecklistItemRequest?>? ChecklistItems);

    public sealed record ServiceChecklistItemRequest(string Description, bool IsRequired);
}
