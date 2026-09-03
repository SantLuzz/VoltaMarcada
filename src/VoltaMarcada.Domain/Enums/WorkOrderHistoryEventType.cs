namespace VoltaMarcada.Domain.Enums;

public enum WorkOrderHistoryEventType
{
    Created,
    DetailsUpdated,
    ProfessionalAssigned,
    Started,
    ChecklistProgressSaved,
    ExecutionNotesUpdated,
    PhotoAdded,
    PhotoRemoved,
    AttachmentAdded,
    AttachmentRemoved,
    AdditionalItemsUpdated,
    Completed,
    Cancelled
}
