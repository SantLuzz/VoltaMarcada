namespace ServiceSync.Application.Customers;

public sealed record CustomerOperationResult(CustomerOperationResultCode Code, Guid? CustomerId)
{
    public static CustomerOperationResult Success(Guid? customerId = null) =>
        new(CustomerOperationResultCode.Success, customerId);

    public static CustomerOperationResult InvalidInput() =>
        new(CustomerOperationResultCode.InvalidInput, null);

    public static CustomerOperationResult InvalidLocation() =>
        new(CustomerOperationResultCode.InvalidLocation, null);

    public static CustomerOperationResult NotFound() =>
        new(CustomerOperationResultCode.NotFound, null);
}

public enum CustomerOperationResultCode
{
    Success,
    InvalidInput,
    InvalidLocation,
    NotFound
}
