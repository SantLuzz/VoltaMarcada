using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.ValueObjects;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class WorkOrderEnhancementsTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AddAttachmentAndHistory_WhenWorkOrderIsInProgress_RecordsImmutableMetadata()
    {
        var workOrder = CreateInProgressWorkOrder();
        var actorUserId = Guid.NewGuid();

        var attachment = workOrder.AddAttachment(
            "companies/company/work-orders/order/attachments/report.pdf",
            "report.pdf",
            "application/pdf",
            1024,
            "Inspection report",
            actorUserId,
            Now);
        workOrder.RecordHistory(
            WorkOrderHistoryEventType.AttachmentAdded,
            actorUserId,
            "Anexo adicionado: report.pdf.",
            Now);

        Assert.Equal("report.pdf", attachment.OriginalFileName);
        Assert.Single(workOrder.Attachments);
        var entry = Assert.Single(workOrder.HistoryEntries);
        Assert.Equal(WorkOrderHistoryEventType.AttachmentAdded, entry.EventType);
        Assert.Equal(actorUserId, entry.ActorUserId);
        Assert.Equal(Now, entry.CreatedAtUtc);
    }

    private static WorkOrder CreateInProgressWorkOrder()
    {
        var companyId = Guid.NewGuid();
        var workOrder = WorkOrder.Create(
            companyId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            WorkOrderPriority.Normal,
            null,
            [new WorkOrderItemDefinition("Maintenance", 1, 150m, WorkOrderItemType.Service)],
            [],
            Now);

        workOrder.Start(Now);
        return workOrder;
    }
}
