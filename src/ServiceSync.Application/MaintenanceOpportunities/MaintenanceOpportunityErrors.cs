using ServiceSync.Application.Abstractions.Operations;

namespace ServiceSync.Application.MaintenanceOpportunities;

public static class MaintenanceOpportunityErrors
{
    public static readonly OperationError InvalidInput =
        new("MaintenanceOpportunity.InvalidInput", OperationErrorType.Validation, "Maintenance opportunity input is invalid.");

    public static readonly OperationError NotFound =
        new("MaintenanceOpportunity.NotFound", OperationErrorType.NotFound, "Maintenance opportunity was not found.");

    public static readonly OperationError InvalidStatus =
        new("MaintenanceOpportunity.InvalidStatus", OperationErrorType.Conflict, "Maintenance opportunity status does not allow this action.");

    public static readonly OperationError Forbidden =
        new("MaintenanceOpportunity.Forbidden", OperationErrorType.Forbidden, "Current user is not allowed to manage maintenance opportunities.");
}
