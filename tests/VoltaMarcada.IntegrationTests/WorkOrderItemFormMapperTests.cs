using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Enums;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class WorkOrderItemFormMapperTests
{
    [Fact]
    public void ToInputs_WithMultipleServiceItems_PreservesEachServiceTypeAndEquipment()
    {
        var equipmentA = Guid.NewGuid();
        var equipmentB = Guid.NewGuid();
        var serviceA = Guid.NewGuid();
        var serviceB = Guid.NewGuid();
        var items = new[]
        {
            new WorkOrderItemDto(Guid.NewGuid(), "Installation", 1, 200m, WorkOrderItemType.Service, 200m, 0, equipmentA, "Air conditioner A", serviceA),
            new WorkOrderItemDto(Guid.NewGuid(), "Installation", 1, 200m, WorkOrderItemType.Service, 200m, 1, equipmentB, "Air conditioner B", serviceB)
        };

        var result = WorkOrderItemFormMapper.ToInputs(items);

        Assert.Collection(
            result,
            item =>
            {
                Assert.Equal(WorkOrderItemType.Service, item.Type);
                Assert.Equal(equipmentA, item.EquipmentId);
                Assert.Equal(serviceA, item.ServiceId);
            },
            item =>
            {
                Assert.Equal(WorkOrderItemType.Service, item.Type);
                Assert.Equal(equipmentB, item.EquipmentId);
                Assert.Equal(serviceB, item.ServiceId);
            });
    }
}
