using VoltaMarcada.Application.Abstractions.Operations;

namespace VoltaMarcada.Application.Equipment;

public static class EquipmentErrors
{
    public static readonly OperationError InvalidInput =
        new("Equipment.InvalidInput", OperationErrorType.Validation, "Equipment input is invalid.");

    public static readonly OperationError CustomerNotFound =
        new("Equipment.CustomerNotFound", OperationErrorType.Validation, "The selected customer is invalid.");

    public static readonly OperationError NotFound =
        new("Equipment.NotFound", OperationErrorType.NotFound, "Equipment was not found.");
}
