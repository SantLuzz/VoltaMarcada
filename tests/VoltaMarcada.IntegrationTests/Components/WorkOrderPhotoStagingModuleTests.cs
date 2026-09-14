using Xunit;

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class WorkOrderPhotoStagingModuleTests
{
    [Fact]
    public void Module_ShouldExposePersistentStageRestoreAndClearOperations()
    {
        var modulePath = Path.Combine(FindRepositoryRoot(), "src", "VoltaMarcada.Web", "wwwroot", "js", "work-order-photo-staging.js");

        Assert.True(File.Exists(modulePath), "The Work Order photo staging module must exist.");

        var source = File.ReadAllText(modulePath);

        Assert.Contains("export async function stagePhoto", source, StringComparison.Ordinal);
        Assert.Contains("export async function restoreStagedPhoto", source, StringComparison.Ordinal);
        Assert.Contains("export async function clearStagedPhoto", source, StringComparison.Ordinal);
        Assert.Contains("indexedDB", source, StringComparison.Ordinal);
        Assert.Contains("transaction.oncomplete", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Module_ShouldRejectUnsupportedFilesAndDiscardCachedFilesAfterAnUploadFailure()
    {
        var modulePath = Path.Combine(FindRepositoryRoot(), "src", "VoltaMarcada.Web", "wwwroot", "js", "work-order-photo-staging.js");

        var source = File.ReadAllText(modulePath);

        Assert.Contains("const supportedContentTypes", source, StringComparison.Ordinal);
        Assert.Contains("export async function discardStagedPhoto", source, StringComparison.Ordinal);
        Assert.Contains("if (!response.ok) { await clearStagedPhoto(workOrderId, photoType);", source, StringComparison.Ordinal);
        Assert.Contains("function showValidationToast", source, StringComparison.Ordinal);
        Assert.Contains("showValidationToast(\"Selecione uma foto JPEG, PNG, WebP ou HEIC de até 10 MB.\")", source, StringComparison.Ordinal);
        Assert.Contains("input.value = \"\";", source, StringComparison.Ordinal);
        Assert.Contains("window.setTimeout(() => toast.remove(), 8000);", source, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "VoltaMarcada.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Unable to find the repository root.");
    }
}
