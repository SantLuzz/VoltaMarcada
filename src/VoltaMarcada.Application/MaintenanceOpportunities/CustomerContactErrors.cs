using VoltaMarcada.Application.Abstractions.Operations;

namespace VoltaMarcada.Application.MaintenanceOpportunities;

public static class CustomerContactErrors
{
    public static readonly OperationError InvalidInput =
        new("CustomerContact.InvalidInput", OperationErrorType.Validation, "Customer contact input is invalid.");

    public static readonly OperationError NotFound =
        new("CustomerContact.NotFound", OperationErrorType.NotFound, "Customer contact was not found.");

    public static readonly OperationError InvalidStatus =
        new("CustomerContact.InvalidStatus", OperationErrorType.Conflict, "Customer contact status does not allow this action.");

    public static readonly OperationError WhatsAppUnavailable =
        new("CustomerContact.WhatsAppUnavailable", OperationErrorType.Validation, "Customer WhatsApp number is unavailable.");

    public static readonly OperationError Forbidden =
        new("CustomerContact.Forbidden", OperationErrorType.Forbidden, "Current user is not allowed to manage customer contacts.");
}
