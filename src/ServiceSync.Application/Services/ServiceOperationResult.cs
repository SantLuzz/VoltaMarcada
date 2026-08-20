namespace ServiceSync.Application.Services;

public sealed record ServiceOperationResult(ServiceOperationResultCode Code, Guid? ServiceId)
{
    public static ServiceOperationResult Success(Guid? serviceId = null) =>
        new(ServiceOperationResultCode.Success, serviceId);

    public static ServiceOperationResult InvalidInput() =>
        new(ServiceOperationResultCode.InvalidInput, null);

    public static ServiceOperationResult NotFound() =>
        new(ServiceOperationResultCode.NotFound, null);
}

public enum ServiceOperationResultCode
{
    Success,
    InvalidInput,
    NotFound
}
