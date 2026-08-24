namespace ServiceSync.Application.Abstractions.Operations;

public sealed record OperationResult(bool IsSuccess, IReadOnlyList<OperationError> Errors)
{
    public OperationError? Error => Errors.Count == 0 ? null : Errors[0];

    public static OperationResult Success() => new(true, []);

    public static OperationResult Failure(params OperationError[] errors) =>
        new(false, Validate(errors));

    private static IReadOnlyList<OperationError> Validate(OperationError[] errors) =>
        errors.Length > 0
            ? errors
            : throw new ArgumentException("At least one operation error is required.", nameof(errors));
}

public sealed record OperationResult<TValue>(
    bool IsSuccess,
    TValue? Value,
    IReadOnlyList<OperationError> Errors)
{
    public OperationError? Error => Errors.Count == 0 ? null : Errors[0];

    public static OperationResult<TValue> Success(TValue value) => new(true, value, []);

    public static OperationResult<TValue> Failure(params OperationError[] errors) =>
        new(false, default, Validate(errors));

    private static IReadOnlyList<OperationError> Validate(OperationError[] errors) =>
        errors.Length > 0
            ? errors
            : throw new ArgumentException("At least one operation error is required.", nameof(errors));
}
