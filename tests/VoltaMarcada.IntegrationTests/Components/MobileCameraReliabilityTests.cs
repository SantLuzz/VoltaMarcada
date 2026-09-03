using Xunit;

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class MobileCameraReliabilityTests
{
    [Fact]
    public void ServerAndWorkOrderPage_ShouldRetainCircuitAndExecutionTab()
    {
        Assert.Contains("DisconnectedCircuitRetentionPeriod", ReadSource("Program.cs"), StringComparison.Ordinal);
        Assert.Contains("ActivePanelIndex", ReadSource("Components/Pages/WorkOrderRegistration.razor"), StringComparison.Ordinal);
        Assert.DoesNotContain("location.reload()", ReadSource("Components/Layout/ReconnectModal.razor.js"), StringComparison.Ordinal);
    }

    [Fact]
    public void CameraCapture_ShouldUseBrowserMultipartUploadInsteadOfCircuitPayload()
    {
        var stagingSource = ReadSource("wwwroot/js/work-order-photo-staging.js");
        var panelSource = ReadSource("Components/Shared/WorkOrderExecutionPanel.razor");

        Assert.Contains("FormData", stagingSource, StringComparison.Ordinal);
        Assert.Contains("credentials: \"same-origin\"", stagingSource, StringComparison.Ordinal);
        Assert.DoesNotContain("Convert.ToBase64String", panelSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ReadPhotoAsync(file)", panelSource, StringComparison.Ordinal);
    }

    [Fact]
    public void CameraCapture_ShouldSendThePhotoTypeAndShowAClientPreview()
    {
        var stagingSource = ReadSource("wwwroot/js/work-order-photo-staging.js");
        var panelSource = ReadSource("Components/Shared/WorkOrderExecutionPanel.razor");

        Assert.Contains("?type=${encodeURIComponent(photoType)}", stagingSource, StringComparison.Ordinal);
        Assert.Contains("showPreview(previewId, file)", stagingSource, StringComparison.Ordinal);
        Assert.Contains("work-order-photo-preview", panelSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Routes_ShouldShowCircularAuthorizationLoadingIndicator()
    {
        Assert.Contains("MudProgressCircular", ReadSource("Components/Routes.razor"), StringComparison.Ordinal);
    }
    [Fact]
    public void PhotoUploadEndpoint_ShouldBindPhotoTypeFromMultipartForm()
    {
        var endpointSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "VoltaMarcada.Web", "Endpoints", "WorkOrderEndpoints.cs"));

        Assert.Contains("[FromForm] WorkOrderPhotoType type", endpointSource, StringComparison.Ordinal);
    }
    private static string ReadSource(string relativePath) => File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "VoltaMarcada.Web", relativePath));

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "VoltaMarcada.sln"))) return directory.FullName;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
