using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.ValueObjects;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class WorkOrderEnhancementsTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithItemsLinkedToDifferentEquipmentAndServices_PreservesEachAssociation()
    {
        var companyId = Guid.NewGuid();
        var equipmentA = Guid.NewGuid();
        var equipmentB = Guid.NewGuid();
        var serviceA = Guid.NewGuid();
        var serviceB = Guid.NewGuid();
        var items = new[]
        {
            new WorkOrderItemDefinition("Clean A", 1, 120m, WorkOrderItemType.Service, equipmentA, serviceA),
            new WorkOrderItemDefinition("Repair B", 1, 150m, WorkOrderItemType.Service, equipmentB, serviceB),
            new WorkOrderItemDefinition("Travel", 1, 30m, WorkOrderItemType.Travel, null, null)
        };

        var workOrder = WorkOrder.Create(
            companyId,
            Guid.NewGuid(),
            equipmentA,
            serviceA,
            Guid.NewGuid(),
            Guid.NewGuid(),
            WorkOrderPriority.Normal,
            null,
            items,
            [],
            Now);

        Assert.Equal(equipmentA, workOrder.Items.ElementAt(0).EquipmentId);
        Assert.Equal(serviceB, workOrder.Items.ElementAt(1).ServiceId);
        Assert.Null(workOrder.Items.ElementAt(2).EquipmentId);
        Assert.Null(workOrder.Items.ElementAt(2).ServiceId);
    }

    [Fact]
    public void UpdatePendingDetails_WithMultipleServiceItems_RetainsEachAssociation()
    {
        var companyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var equipmentA = Guid.NewGuid();
        var equipmentB = Guid.NewGuid();
        var serviceA = Guid.NewGuid();
        var serviceB = Guid.NewGuid();
        var items = new[]
        {
            new WorkOrderItemDefinition("Clean A", 1, 120m, WorkOrderItemType.Service, equipmentA, serviceA),
            new WorkOrderItemDefinition("Repair B", 1, 150m, WorkOrderItemType.Service, equipmentB, serviceB),
            new WorkOrderItemDefinition("Travel", 1, 30m, WorkOrderItemType.Travel, null, null)
        };

        var workOrder = WorkOrder.Create(
            companyId,
            customerId,
            equipmentA,
            serviceA,
            Guid.NewGuid(),
            Guid.NewGuid(),
            WorkOrderPriority.Normal,
            null,
            items,
            [],
            Now);

        workOrder.UpdatePendingDetails(
            customerId,
            equipmentA,
            serviceA,
            workOrder.ProfessionalId,
            WorkOrderPriority.Normal,
            null,
            items,
            [],
            10m,
            Now.AddMinutes(1));

        Assert.Equal(equipmentA, workOrder.Items.ElementAt(0).EquipmentId);
        Assert.Equal(serviceA, workOrder.Items.ElementAt(0).ServiceId);
        Assert.Equal(equipmentB, workOrder.Items.ElementAt(1).EquipmentId);
        Assert.Equal(serviceB, workOrder.Items.ElementAt(1).ServiceId);
    }


    [Fact]
    public void UpdatePendingDetails_WithAnAdditionalServiceItem_AllowsTheServiceAndPreservesItsAssociation()
    {
        var companyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var equipmentA = Guid.NewGuid();
        var equipmentB = Guid.NewGuid();
        var serviceA = Guid.NewGuid();
        var serviceB = Guid.NewGuid();
        var primaryItem = new WorkOrderItemDefinition("Clean A", 1, 120m, WorkOrderItemType.Service, equipmentA, serviceA);
        var additionalServiceItem = new WorkOrderItemDefinition("Repair B", 1, 150m, WorkOrderItemType.Service, equipmentB, serviceB);
        var workOrder = WorkOrder.Create(companyId, customerId, equipmentA, serviceA, Guid.NewGuid(), Guid.NewGuid(), WorkOrderPriority.Normal, null, [primaryItem], [], Now);

        workOrder.UpdatePendingDetails(
            customerId,
            equipmentA,
            serviceA,
            workOrder.ProfessionalId,
            WorkOrderPriority.Normal,
            null,
            [primaryItem, additionalServiceItem],
            [],
            0m,
            Now.AddMinutes(1));

        var additionalService = Assert.Single(workOrder.Items, item => item.ServiceId == serviceB);
        Assert.Equal(equipmentB, additionalService.EquipmentId);
        Assert.Equal(WorkOrderItemType.Service, additionalService.Type);
    }

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

    [Fact]
    public void UpdatePendingDetails_WhenDiscountChanges_UpdatesTheFinalTotal()
    {
        var companyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var equipmentId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var workOrder = WorkOrder.Create(
            companyId, customerId, equipmentId, serviceId, Guid.NewGuid(), Guid.NewGuid(),
            WorkOrderPriority.Normal, null,
            [new WorkOrderItemDefinition("Maintenance", 1, 150m, WorkOrderItemType.Service)],
            [],
            Now,
            discountAmount: 10m);

        workOrder.UpdatePendingDetails(
            customerId,
            equipmentId,
            serviceId,
            workOrder.ProfessionalId,
            WorkOrderPriority.Normal,
            null,
            [new WorkOrderItemDefinition("Maintenance", 1, 150m, WorkOrderItemType.Service)],
            [],
            25m,
            Now.AddMinutes(1));

        Assert.Equal(25m, workOrder.DiscountAmount);
        Assert.Equal(125m, workOrder.TotalAmount);
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
