using Xunit;

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class EquipmentHistoryLocalizationTests
{
    [Fact]
    public void EquipmentHistoryDrawer_DoesNotContainReplacementQuestionMarksInPortugueseUiText()
    {
        var source = File.ReadAllText(GetSourcePath("Components", "Shared", "EquipmentHistoryDrawer.razor"));
        var equipmentSource = File.ReadAllText(GetSourcePath("Components", "Pages", "Equipment.razor"));

        Assert.DoesNotContain("Hist?rico", source, StringComparison.Ordinal);
        Assert.DoesNotContain("T?cnico", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Pr?xima", source, StringComparison.Ordinal);
        Assert.DoesNotContain("N?o foi poss?vel", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Ver hist?rico t?cnico", equipmentSource, StringComparison.Ordinal);
    }

    private static string GetSourcePath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "VoltaMarcada.sln")))
            {
                return Path.Combine(directory.FullName, "src", "VoltaMarcada.Web", Path.Combine(segments));
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("The solution root could not be located.");
    }
}
