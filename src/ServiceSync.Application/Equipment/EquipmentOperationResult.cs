namespace ServiceSync.Application.Equipment;

public sealed record EquipmentOperationResult(EquipmentOperationResultCode Code, Guid? EquipmentId)
{
    public static EquipmentOperationResult Success(Guid? equipmentId = null) =>
        new(EquipmentOperationResultCode.Success, equipmentId);

    public static EquipmentOperationResult InvalidInput() =>
        new(EquipmentOperationResultCode.InvalidInput, null);

    public static EquipmentOperationResult CustomerNotFound() =>
        new(EquipmentOperationResultCode.CustomerNotFound, null);

    public static EquipmentOperationResult NotFound() =>
        new(EquipmentOperationResultCode.NotFound, null);
}

public enum EquipmentOperationResultCode
{
    Success,
    InvalidInput,
    CustomerNotFound,
    NotFound
}
