using VoltaMarcada.Application.Equipment;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class EquipmentDisplaySummaryTests
{
    [Fact]
    public void GetCompactDescription_WhenMultipleEquipmentAreSelected_ReturnsSelectedEquipmentCount()
    {
        var firstEquipmentId = Guid.NewGuid();
        var secondEquipmentId = Guid.NewGuid();

        var description = EquipmentDisplaySummary.GetCompactDescription(
            [firstEquipmentId, secondEquipmentId, firstEquipmentId],
            new Dictionary<Guid, string>
            {
                [firstEquipmentId] = "Air conditioner A",
                [secondEquipmentId] = "Air conditioner B"
            },
            "Fallback equipment");

        Assert.Equal("2 equipamentos selecionados", description);
    }
}
