namespace ServiceSync.Application.WorkOrders;

public sealed record WorkOrderOperationValue(Guid? WorkOrderId = null, Guid? PhotoId = null);
