namespace ServiceSync.Application.WorkOrders;

public sealed record WorkOrderOperationResult(WorkOrderOperationResultCode Code, Guid? WorkOrderId = null, Guid? PhotoId = null)
{
    public static WorkOrderOperationResult Success(Guid? workOrderId = null, Guid? photoId = null) => new(WorkOrderOperationResultCode.Success, workOrderId, photoId);
    public static WorkOrderOperationResult Failure(WorkOrderOperationResultCode code) => new(code);
}

public enum WorkOrderOperationResultCode
{
    Success, InvalidInput, NotFound, Forbidden, CustomerNotFound, EquipmentNotFound, InvalidEquipmentCustomer, ServiceNotFound, ProfessionalNotFound, ProfessionalNotQualified, ProfessionalProfileRequired, ProfessionalRequired, ProfessionalUnavailable, InvalidStatus, ChecklistIncomplete, BeforePhotoRequired, AfterPhotoRequired, OpenAppointments, PhotoLimitReached, InvalidPhoto
}
