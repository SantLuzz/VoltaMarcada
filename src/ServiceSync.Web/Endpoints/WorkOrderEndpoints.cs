using ServiceSync.Application.WorkOrders;
using ServiceSync.Domain.Enums;
using ServiceSync.Web.Authorization;

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
        group.MapPut("/{workOrderId:guid}", async (Guid workOrderId, UpdateWorkOrderCommand command, UpdateWorkOrderUseCase useCase, CancellationToken cancellationToken) => MapOperationResult(await useCase.ExecuteAsync(workOrderId, command, cancellationToken)));
        group.MapDelete("/{workOrderId:guid}", async (Guid workOrderId, CancelWorkOrderUseCase useCase, CancellationToken cancellationToken) => MapOperationResult(await useCase.ExecuteAsync(workOrderId, cancellationToken)));
        group.MapPost("/{workOrderId:guid}/start", async (Guid workOrderId, StartWorkOrderUseCase useCase, CancellationToken cancellationToken) => MapOperationResult(await useCase.ExecuteAsync(workOrderId, cancellationToken)));
        group.MapPut("/{workOrderId:guid}/checklist", async (Guid workOrderId, SaveWorkOrderChecklistProgressCommand command, SaveWorkOrderChecklistProgressUseCase useCase, CancellationToken cancellationToken) => MapOperationResult(await useCase.ExecuteAsync(workOrderId, command, cancellationToken)));
        group.MapPost("/{workOrderId:guid}/complete", async (Guid workOrderId, CompleteWorkOrderUseCase useCase, CancellationToken cancellationToken) => MapOperationResult(await useCase.ExecuteAsync(workOrderId, cancellationToken)));
        group.MapPost("/{workOrderId:guid}/photos", async (Guid workOrderId, IFormFile file, WorkOrderPhotoType type, string? description, AddWorkOrderPhotoUseCase useCase, CancellationToken cancellationToken) =>
        {
            if (file.Length > 10 * 1024 * 1024) return Results.BadRequest(new { error = "A foto excede o limite de 10 MB." });
            await using var stream = file.OpenReadStream(10 * 1024 * 1024 + 1);
            var result = await useCase.ExecuteAsync(workOrderId, new WorkOrderPhotoUpload(stream, file.FileName, file.ContentType, file.Length, type, description), cancellationToken);
            return MapOperationResult(result);
        });
        group.MapGet("/{workOrderId:guid}/photos/{photoId:guid}/content", async (Guid workOrderId, Guid photoId, GetWorkOrderPhotoContentUseCase useCase, CancellationToken cancellationToken) =>
        {
            var content = await useCase.ExecuteAsync(workOrderId, photoId, cancellationToken);
            return content is null ? Results.NotFound() : Results.File(content.Content, content.ContentType);
        });
        group.MapDelete("/{workOrderId:guid}/photos/{photoId:guid}", async (Guid workOrderId, Guid photoId, RemoveWorkOrderPhotoUseCase useCase, CancellationToken cancellationToken) => MapOperationResult(await useCase.ExecuteAsync(workOrderId, photoId, cancellationToken)));
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(CreateWorkOrderCommand? command, CreateWorkOrderUseCase useCase, CancellationToken cancellationToken)
    {
        if (command is null) return Results.BadRequest(new { error = "Revise os dados da ordem de serviço." });
        var result = await useCase.ExecuteAsync(command, cancellationToken);
        return result.Code == WorkOrderOperationResultCode.Success ? Results.Created($"/work-orders/{result.WorkOrderId}", new { workOrderId = result.WorkOrderId }) : MapOperationResult(result);
    }

    private static IResult MapOperationResult(WorkOrderOperationResult result) => result.Code switch
    {
        WorkOrderOperationResultCode.Success => Results.Ok(result),
        WorkOrderOperationResultCode.NotFound or WorkOrderOperationResultCode.CustomerNotFound or WorkOrderOperationResultCode.EquipmentNotFound or WorkOrderOperationResultCode.ServiceNotFound or WorkOrderOperationResultCode.ProfessionalNotFound => Results.NotFound(),
        WorkOrderOperationResultCode.Forbidden => Results.Forbid(),
        _ => Results.BadRequest(new { error = GetErrorMessage(result.Code) })
    };

    private static string GetErrorMessage(WorkOrderOperationResultCode code) => code switch
    {
        WorkOrderOperationResultCode.InvalidEquipmentCustomer => "O equipamento não pertence ao cliente selecionado.",
        WorkOrderOperationResultCode.ProfessionalProfileRequired => "É necessário possuir um perfil profissional ativo.",
        WorkOrderOperationResultCode.ProfessionalNotQualified => "O profissional não está habilitado para o serviço.",
        WorkOrderOperationResultCode.ChecklistIncomplete => "Conclua os itens obrigatórios do checklist.",
        WorkOrderOperationResultCode.BeforePhotoRequired => "Inclua ao menos uma foto antes.",
        WorkOrderOperationResultCode.AfterPhotoRequired => "Inclua ao menos uma foto depois.",
        WorkOrderOperationResultCode.PhotoLimitReached => "O limite de fotos da ordem de serviço foi atingido.",
        WorkOrderOperationResultCode.InvalidPhoto => "Envie uma imagem JPEG, PNG, WebP ou HEIC de até 10 MB.",
        _ => "Não foi possível concluir a operação da ordem de serviço."
    };
}
