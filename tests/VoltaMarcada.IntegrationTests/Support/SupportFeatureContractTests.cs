using VoltaMarcada.Application.Support;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Support;

public sealed class SupportFeatureContractTests
{
    [Fact]
    public void AttachmentDetector_RecognizesOnlyExpectedImageSignatures()
    {
        Assert.Equal("image/png", SupportAttachmentDetector.DetectContentType([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]));
        Assert.Equal("image/jpeg", SupportAttachmentDetector.DetectContentType([0xFF, 0xD8, 0xFF]));
        Assert.Equal("image/webp", SupportAttachmentDetector.DetectContentType([0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50]));
        Assert.Null(SupportAttachmentDetector.DetectContentType("not an image"u8.ToArray()));
    }
}
