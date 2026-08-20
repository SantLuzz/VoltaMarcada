using ServiceSync.Application.Services;
using ServiceSync.Domain.Enums;
using ServiceSync.Web.Authorization;

namespace ServiceSync.Web.Endpoints;

public static class ServiceEndpoints
{
    public static IEndpointRouteBuilder MapServiceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var services = endpoints.MapGroup("/services")
            .RequireAuthorization(AuthorizationPolicies.CompanyMember);

        services.MapPost("/", CreateAsync);
        services.MapGet("/", GetAllAsync);
        services.MapGet("/{serviceId:guid}/edit", GetForEditAsync);
        services.MapPut("/{serviceId:guid}", UpdateAsync);
        services.MapDelete("/{serviceId:guid}", DeleteAsync);

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
        return ToResult(result, serviceId => Results.Created($"/services/{serviceId}/edit", new { serviceId }));
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
        return ToResult(result, _ => Results.Ok());
    }

    private static async Task<IResult> DeleteAsync(
        Guid serviceId,
        DeleteServiceUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(serviceId, cancellationToken);
        return result.Code switch
        {
            ServiceOperationResultCode.Success => Results.NoContent(),
            ServiceOperationResultCode.NotFound => Results.NotFound(),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    private static IResult ToResult(ServiceOperationResult result, Func<Guid, IResult> success) =>
        result.Code switch
        {
            ServiceOperationResultCode.Success when result.ServiceId is { } serviceId => success(serviceId),
            ServiceOperationResultCode.InvalidInput => Results.BadRequest(
                new { error = "Revise os dados do serviço e tente novamente." }),
            ServiceOperationResultCode.NotFound => Results.NotFound(),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
        };

    private static IResult InvalidInput() =>
        Results.BadRequest(new { error = "Revise os dados do serviço e tente novamente." });

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
