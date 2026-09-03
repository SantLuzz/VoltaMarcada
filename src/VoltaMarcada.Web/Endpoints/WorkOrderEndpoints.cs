using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Web.Authorization;
using VoltaMarcada.Web.Errors;

namespace VoltaMarcada.Web.Endpoints;

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
        group.MapPost("/{workOrderId:guid}/attachments", async (Guid workOrderId, IFormFile file, string? description, AddWorkOrderAttachmentUseCase useCase, CancellationToken cancellationToken) =>
        {
            if (file.Length > 10 * 1024 * 1024) return OperationResultHttpExtensions.ValidationProblem(WorkOrderErrors.InvalidAttachment.Code, OperationErrorMessages.GetMessage(WorkOrderErrors.InvalidAttachment.Code));
            await using var stream = file.OpenReadStream();
            return ToOperationHttpResult(await useCase.ExecuteAsync(workOrderId, new WorkOrderAttachmentUpload(stream, file.FileName, file.ContentType, file.Length, description), cancellationToken));
        });
        group.MapGet("/{workOrderId:guid}/attachments/{attachmentId:guid}/content", async (Guid workOrderId, Guid attachmentId, GetWorkOrderAttachmentContentUseCase useCase, CancellationToken cancellationToken) =>
        {
            var content = await useCase.ExecuteAsync(workOrderId, attachmentId, cancellationToken);
            return content is null ? Results.NotFound() : Results.File(content.Content, content.ContentType);
        });
        group.MapDelete("/{workOrderId:guid}/attachments/{attachmentId:guid}", async (Guid workOrderId, Guid attachmentId, RemoveWorkOrderAttachmentUseCase useCase, CancellationToken cancellationToken) => ToOperationHttpResult(await useCase.ExecuteAsync(workOrderId, attachmentId, cancellationToken)));
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
