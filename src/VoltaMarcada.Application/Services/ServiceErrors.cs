using VoltaMarcada.Application.Abstractions.Operations;

namespace VoltaMarcada.Application.Services;

public static class ServiceErrors
{
    public static readonly OperationError InvalidInput =
        new("Service.InvalidInput", OperationErrorType.Validation, "Service input is invalid.");

    public static readonly OperationError NotFound =
        new("Service.NotFound", OperationErrorType.NotFound, "Service was not found.");
}
