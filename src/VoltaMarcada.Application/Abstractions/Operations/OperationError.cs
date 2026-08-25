namespace VoltaMarcada.Application.Abstractions.Operations;

public sealed record OperationError(
    string Code,
    OperationErrorType Type,
    string Description,
    string? Field = null);
