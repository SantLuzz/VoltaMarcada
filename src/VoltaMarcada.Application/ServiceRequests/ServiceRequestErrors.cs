using VoltaMarcada.Application.Abstractions.Operations;

namespace VoltaMarcada.Application.ServiceRequests;

public static class ServiceRequestErrors
{
    public static readonly OperationError NotFound = new("ServiceRequest.NotFound", OperationErrorType.NotFound, "Service request was not found.");
    public static readonly OperationError InvalidInput = new("ServiceRequest.InvalidInput", OperationErrorType.Validation, "Service request input is invalid.");
    public static readonly OperationError InvalidStatus = new("ServiceRequest.InvalidStatus", OperationErrorType.Conflict, "Service request state does not allow this action.");
    public static readonly OperationError Forbidden = new("ServiceRequest.Forbidden", OperationErrorType.Forbidden, "Service request access is forbidden.");
    public static readonly OperationError ProfessionalUnavailable = new("ServiceRequest.ProfessionalUnavailable", OperationErrorType.Conflict, "Professional is unavailable.");
}
