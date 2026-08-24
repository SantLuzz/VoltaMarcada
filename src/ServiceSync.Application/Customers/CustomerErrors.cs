using ServiceSync.Application.Abstractions.Operations;

namespace ServiceSync.Application.Customers;

public static class CustomerErrors
{
    public static readonly OperationError InvalidInput =
        new("Customer.InvalidInput", OperationErrorType.Validation, "Customer input is invalid.");

    public static readonly OperationError InvalidLocation =
        new("Customer.InvalidLocation", OperationErrorType.Validation, "Customer location is invalid.");

    public static readonly OperationError DuplicateDocument =
        new("Customer.DuplicateDocument", OperationErrorType.Conflict, "A customer with this document already exists.");

    public static readonly OperationError NotFound =
        new("Customer.NotFound", OperationErrorType.NotFound, "Customer was not found.");

    public static readonly OperationError HasLinkedEquipment =
        new("Customer.HasLinkedEquipment", OperationErrorType.Conflict, "Customer has linked equipment.");
}
