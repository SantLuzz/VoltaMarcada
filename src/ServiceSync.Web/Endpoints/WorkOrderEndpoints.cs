using ServiceSync.Application.Abstractions.Operations;
using ServiceSync.Application.WorkOrders;
using ServiceSync.Domain.Enums;
using ServiceSync.Web.Authorization;
using ServiceSync.Web.Errors;

namespace ServiceSync.Web.Endpoints;

public static class WorkOrderEndpoints
{
    public static IEndpointRouteBuilder MapWorkOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/work-orders").RequireAuthorization(AuthorizationPolicies.CompanyMember);
        group.MapGet("/", async (string? search, GetWorkOrdersUseCase useCase, CancellationToken cancellationToken) => Results.Ok(await useCase.ExecuteAsync(new GetWorkOrdersQuery(search, null), cancellationToken)));
        group.MapPost("/", CreateAsync);
        group.MapGet("/{workOrderId:guid}", async (Guid workOrderId, GetWorkOrderDetailsUseCase useCase, CancellationToken cancellationToken) =>
        {
            var detail = await useCase.ExecuteAsync(workOrderId, cancellationToken);
            return detail is null ? Results.NotFound() : Results.Ok(detail);
        });
        group.MapPut("/{workOrderId:guid}", async (Guid workOrderId, UpdateWorkOrderCommand command, UpdateWorkOrderUseCase useCase, CancellationToken cancellationToken) => ToOperationHttpResult(await useCase.ExecuteAsync(workOrderId, command, cancellationToken)));
        group.MapDelete("/{workOrderId:guid}", async (Guid workOrderId, CancelWorkOrderUseCase useCase, CancellationToken cancellationToken) => ToOperationHttpResult(await useCase.ExecuteAsync(workOrderId, cancellationToken)));
        group.MapPost("/{workOrderId:guid}/start", async (Guid workOrderId, StartWorkOrderUseCase useCase, CancellationToken cancellationToken) => ToOperationHttpResult(await useCase.ExecuteAsync(workOrderId, cancellationToken)));
        group.MapPut("/{workOrderId:guid}/checklist", async (Guid workOrderId, SaveWorkOrderChecklistProgressCommand command, SaveWorkOrderChecklistProgressUseCase useCase, CancellationToken cancellationToken) => ToOperationHttpResult(await useCase.ExecuteAsync(workOrderId, command, cancellationToken)));
        group.MapPost("/{workOrderId:guid}/complete", async (Guid workOrderId, CompleteWorkOrderCommand? command, CompleteWorkOrderUseCase useCase, CancellationToken cancellationToken) => ToOperationHttpResult(await useCase.ExecuteAsync(workOrderId, command ?? new CompleteWorkOrderCommand(), cancellationToken)));
        group.MapPost("/{workOrderId:guid}/photos", async (Guid workOrderId, IFormFile file, WorkOrderPhotoType type, string? description, AddWorkOrderPhotoUseCase useCase, CancellationToken cancellationToken) =>
        {
            if (file.Length > 10 * 1024 * 1024)
            {
                return OperationResultHttpExtensions.ValidationProblem(
                    WorkOrderErrors.InvalidPhoto.Code,
                    OperationErrorMessages.GetMessage(WorkOrderErrors.InvalidPhoto.Code));
            }
            await using var stream = file.OpenReadStream();
            var result = await useCase.ExecuteAsync(workOrderId, new WorkOrderPhotoUpload(stream, file.FileName, file.ContentType, file.Length, type, description), cancellationToken);
            return ToOperationHttpResult(result);
        });
        group.MapGet("/{workOrderId:guid}/photos/{photoId:guid}/content", async (Guid workOrderId, Guid photoId, GetWorkOrderPhotoContentUseCase useCase, CancellationToken cancellationToken) =>
        {
            var content = await useCase.ExecuteAsync(workOrderId, photoId, cancellationToken);
            return content is null ? Results.NotFound() : Results.File(content.Content, content.ContentType);
        });
        group.MapDelete("/{workOrderId:guid}/photos/{photoId:guid}", async (Guid workOrderId, Guid photoId, RemoveWorkOrderPhotoUseCase useCase, CancellationToken cancellationToken) => ToOperationHttpResult(await useCase.ExecuteAsync(workOrderId, photoId, cancellationToken)));
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateWorkOrderCommand? command,
        CreateWorkOrderUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (command is null)
        {
            return OperationResultHttpExtensions.ValidationProblem(
                WorkOrderErrors.InvalidInput.Code,
                OperationErrorMessages.GetMessage(WorkOrderErrors.InvalidInput.Code));
        }

        var result = await useCase.ExecuteAsync(command, cancellationToken);
        return result.ToHttpResult(value => Results.Created(
            $"/work-orders/{value.WorkOrderId}",
            new { workOrderId = value.WorkOrderId }));
    }

    private static IResult ToOperationHttpResult(
        OperationResult<WorkOrderOperationValue> result) =>
        result.ToHttpResult(value => Results.Ok(new
        {
            Code = 0,
            value.WorkOrderId,
            value.PhotoId
        }));
}
