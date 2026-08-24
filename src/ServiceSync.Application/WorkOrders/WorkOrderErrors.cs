using ServiceSync.Application.Abstractions.Operations;

namespace ServiceSync.Application.WorkOrders;

public static class WorkOrderErrors
{
    public static readonly OperationError InvalidInput =
        new("WorkOrder.InvalidInput", OperationErrorType.Validation, "Work order input is invalid.");
    public static readonly OperationError NotFound =
        new("WorkOrder.NotFound", OperationErrorType.NotFound, "Work order was not found.");
    public static readonly OperationError Forbidden =
        new("WorkOrder.Forbidden", OperationErrorType.Forbidden, "The current user cannot perform this work order operation.");
    public static readonly OperationError CustomerNotFound =
        new("WorkOrder.CustomerNotFound", OperationErrorType.NotFound, "Customer was not found.");
    public static readonly OperationError EquipmentNotFound =
        new("WorkOrder.EquipmentNotFound", OperationErrorType.NotFound, "Equipment was not found.");
    public static readonly OperationError InvalidEquipmentCustomer =
        new("WorkOrder.InvalidEquipmentCustomer", OperationErrorType.Validation, "Equipment does not belong to the selected customer.");
    public static readonly OperationError ServiceNotFound =
        new("WorkOrder.ServiceNotFound", OperationErrorType.NotFound, "Service was not found.");
    public static readonly OperationError ProfessionalNotFound =
        new("WorkOrder.ProfessionalNotFound", OperationErrorType.NotFound, "Professional was not found.");
    public static readonly OperationError ProfessionalNotQualified =
        new("WorkOrder.ProfessionalNotQualified", OperationErrorType.Validation, "Professional is not qualified for the selected service.");
    public static readonly OperationError ProfessionalProfileRequired =
        new("WorkOrder.ProfessionalProfileRequired", OperationErrorType.Validation, "An active professional profile is required.");
    public static readonly OperationError ProfessionalRequired =
        new("WorkOrder.ProfessionalRequired", OperationErrorType.Validation, "A professional is required for the scheduled work order.");
    public static readonly OperationError ProfessionalUnavailable =
        new("WorkOrder.ProfessionalUnavailable", OperationErrorType.Conflict, "Professional is unavailable for the selected interval.");
    public static readonly OperationError InvalidStatus =
        new("WorkOrder.InvalidStatus", OperationErrorType.Conflict, "Work order does not allow this operation in its current state.");
    public static readonly OperationError ChecklistIncomplete =
        new("WorkOrder.ChecklistIncomplete", OperationErrorType.Conflict, "Required checklist items are incomplete.");
    public static readonly OperationError BeforePhotoRequired =
        new("WorkOrder.BeforePhotoRequired", OperationErrorType.Conflict, "A before photo is required.");
    public static readonly OperationError AfterPhotoRequired =
        new("WorkOrder.AfterPhotoRequired", OperationErrorType.Conflict, "An after photo is required.");
    public static readonly OperationError OpenAppointments =
        new("WorkOrder.OpenAppointments", OperationErrorType.Conflict, "Work order has open appointments.");
    public static readonly OperationError PhotoLimitReached =
        new("WorkOrder.PhotoLimitReached", OperationErrorType.Conflict, "Work order photo limit was reached.");
    public static readonly OperationError InvalidPhoto =
        new("WorkOrder.InvalidPhoto", OperationErrorType.Validation, "Work order photo is invalid.");
    public static readonly OperationError InvalidMaintenanceRecommendation = InvalidInput;
}
