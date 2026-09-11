using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.ValueObjects;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class WorkOrderNoChargeTests
{
    [Fact]
    public void RecordNoCharge_WhenReasonIsProvided_PreservesTheReasonAndActor()
    {
        var now = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);
        var workOrder = WorkOrder.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), WorkOrderPriority.Normal, null,
            [new WorkOrderItemDefinition("Maintenance", 1, 100m, WorkOrderItemType.Service)], [], now);
        var actorId = Guid.NewGuid();

        workOrder.RecordNoCharge("Warranty service", actorId, now.AddMinutes(1));

        Assert.Equal("Warranty service", workOrder.NoChargeReason);
        Assert.Equal(actorId, workOrder.NoChargeRecordedByUserId);
        Assert.Equal(now.AddMinutes(1), workOrder.NoChargeRecordedAtUtc);
    }
}
