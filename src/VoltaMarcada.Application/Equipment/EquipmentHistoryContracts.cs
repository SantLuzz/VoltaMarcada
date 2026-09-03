namespace VoltaMarcada.Application.Equipment;

public sealed record EquipmentHistoryHeaderDto(
    Guid EquipmentId,
    string Description,
    string CustomerName,
    string? LocationDescription);

public sealed record EquipmentHistoryRecordDto(
    string WorkOrderDisplayNumber,
    string ServiceName,
    string? ProfessionalName,
    DateTimeOffset CompletedAtUtc,
    int RequiredChecklistCount,
    int CompletedChecklistCount,
    int BeforePhotoCount,
    int AfterPhotoCount,
    int AttachmentCount,
    decimal TotalAmount,
    string? ExecutionNotes,
    DateOnly? NextMaintenanceDueDate);

public sealed record EquipmentHistoryPageProjection(
    EquipmentHistoryHeaderDto Equipment,
    IReadOnlyList<EquipmentHistoryRecordDto> Records,
    int TotalRecordCount);

public sealed record EquipmentHistoryPageDto(
    EquipmentHistoryHeaderDto Equipment,
    IReadOnlyList<EquipmentHistoryRecordDto> Records,
    int Offset,
    int TotalRecordCount)
{
    public int LoadedRecordCount => Offset + Records.Count;

    public bool HasMore => LoadedRecordCount < TotalRecordCount;
}
